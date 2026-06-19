using ClosedXML.Excel;
using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Shared.BusinessLogic;
using ERPSystem.Shared.Notifications;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Enums;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;
using static ERPSystem.Utils.Constants.General.Route;


namespace ERPSystem.Modules.Contracts;

public class ContractsService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ContractsService> _logger;
    private readonly PdfService _pdfService;
    private readonly NotificationsService _notificationsService;
    private readonly ActivityLogService _activityLogService;
    private readonly ContractInstallmentService _installmentService;
    private readonly ContractPricingService _pricingService;
    private readonly TemplateRendererService _templateRenderer;
    private readonly ExcelExportService _excelExportService;
    private readonly DocumentSigningService _documentSigningService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ContractsService(  
        ApplicationDbContext db,  
        ILogger<ContractsService> logger, 
        PdfService pdfService, 
        NotificationsService notificationsService,
        ActivityLogService activityLogService,
        ContractInstallmentService installmentService,
        ContractPricingService pricingService,
        TemplateRendererService templateRenderer,
        ExcelExportService excelExportService,
        DocumentSigningService documentSigningService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _logger = logger;
        _pdfService = pdfService;
        _notificationsService = notificationsService;
        _activityLogService = activityLogService;
        _installmentService = installmentService;
        _pricingService = pricingService;
        _templateRenderer = templateRenderer;
        _excelExportService = excelExportService;
        _documentSigningService = documentSigningService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PublicResponse> CreateAsync(CreateContractDto dto)
    {
        var response = new PublicResponse(true);

        try
        {
            if (dto.StudentId <= 0 || !dto.CourseSessionIds.Any())
                return response.SetError(  ErrorCodes.InvalidParameters,"Cursantul sau sesiunile de curs sunt invalide."
                );

            if (!dto.IsUnlimited && dto.EndDate == null)
                return response.SetError( ErrorCodes.InvalidParameters, "Data de sfârșit este obligatorie."
                );

            if (!dto.IsUnlimited && dto.EndDate < dto.StartDate)
                return response.SetError( ErrorCodes.InvalidParameters, "Perioada contractului este invalidă."
                );

            var student = await _db.Students
                .FirstOrDefaultAsync(x => x.Id == dto.StudentId);

            if (student is null)
                return response.SetError( ErrorCodes.InvalidParameters, "Cursantul nu a fost găsit."
                );

            var blockingStatuses = new[]
            {
                ContractStatus.Draft,
                ContractStatus.Finalized,
                ContractStatus.SentToClient,
                ContractStatus.SignedByClient,
                ContractStatus.Active
            };

            var existingContract = await _db.StudentContracts
                .Where(c =>
                    blockingStatuses.Contains(c.Status) &&
                    c.Parties.Any(p =>
                        p.StudentId == dto.StudentId))
                .OrderByDescending(c => c.CreatedAtUtc)
                .Select(c => new { c.Id })
                .FirstOrDefaultAsync();

            if (existingContract is not null)
            {
                return response.SetSuccess(new
                {
                    existingContractId = existingContract.Id
                });
            }

            Guardian? guardian = null;

            if (dto.GuardianId.HasValue)
            {
                guardian = await _db.Guardians
                    .FirstOrDefaultAsync(x => x.Id == dto.GuardianId.Value);

                if (guardian is null)
                    return response.SetError(  ErrorCodes.InvalidParameters, "Tutorele nu a fost găsit."
                    );
            }

            if (student.IsMinor)
            {
                if (guardian is null)
                    return response.SetError( ErrorCodes.InvalidParameters, "Pentru un cursant minor este necesar un tutore."
                    );

                var guardianLinked = await _db.StudentGuardians
                    .AnyAsync(sg =>
                        sg.StudentId == student.Id &&
                        sg.GuardianId == guardian.Id);

                if (!guardianLinked)
                    return response.SetError(  ErrorCodes.InvalidParameters, "Tutorele nu este asociat cursantului selectat."
                    );
            }

            var sessions = await _db.CourseSessions
                .Include(x => x.Course)
                .Where(x => dto.CourseSessionIds.Contains(x.Id))
                .ToListAsync();

            if (sessions.Count != dto.CourseSessionIds.Count)
                return response.SetError(  ErrorCodes.InvalidParameters, "Una sau mai multe sesiuni sunt invalide."
                );

            var contract = new StudentContract
            {
                ContractNumber = GenerateContractNumber(),
                StartDate = dto.StartDate,
                EndDate = dto.IsUnlimited ? null : dto.EndDate,
                IsUnlimited = dto.IsUnlimited,
                Installments = dto.Installments <= 0 ? 1 : dto.Installments,
                Status = ContractStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };


            var company = await _db.CompanySettings.FirstAsync();

            contract.CompanyNameSnapshot = company.Name;
            contract.CompanyAddressSnapshot = company.Address;
            contract.CompanyCuiSnapshot = company.CUI;
            contract.CompanyRegistrationSnapshot = company.RegistrationNumber;
            contract.CompanyIbanSnapshot = company.IBAN;
            contract.CompanyBankSnapshot = company.Bank;
            contract.CompanyEmailSnapshot = company.Email;
            contract.CompanyPhoneSnapshot = company.Phone;


            if (guardian != null)
            {
                contract.Parties.Add(new ContractParty
                {
                    GuardianId = guardian.Id,
                    Role = ContractPartyRole.Guardian
                });
            }

            contract.Parties.Add(new ContractParty
            {
                StudentId = student.Id,
                Role = ContractPartyRole.Student
            });


            if (guardian != null)
            {
                contract.BeneficiaryNameSnapshot =
                    $"{guardian.FirstName} {guardian.LastName}";

                contract.BeneficiaryEmailSnapshot = guardian.Email;
                contract.BeneficiaryPhoneSnapshot = guardian.Phone;

                contract.BeneficiaryAddressSnapshot =
                    !string.IsNullOrWhiteSpace(guardian.Address)
                        ? guardian.Address
                        : student.Address;
            }
            else
            {
                contract.BeneficiaryNameSnapshot =
                    $"{student.FirstName} {student.LastName}";

                contract.BeneficiaryEmailSnapshot = student.Email;
                contract.BeneficiaryPhoneSnapshot = student.Phone;
                contract.BeneficiaryAddressSnapshot = student.Address;
            }

            
            Data.Entities.MarketingCampaign? marketingCampaign = null;

            if (dto.MarketingCampaignId.HasValue)
            {
                marketingCampaign = await _db.MarketingCampaigns
                    .Include(x => x.CourseSessions)
                    .FirstOrDefaultAsync(x =>
                        x.Id == dto.MarketingCampaignId.Value &&
                        x.IsActive &&
                        x.StartDate <= DateTime.Today &&
                        x.EndDate >= DateTime.Today);

                if (marketingCampaign is null)
                    return response.SetError(
                        ErrorCodes.InvalidParameters,
                        "Campania nu este validă sau nu mai este activă."
                    );

                var selectedSessionIds = sessions.Select(x => x.Id).ToList();

                var campaignSessionIds = marketingCampaign.CourseSessions
                    .Select(x => x.CourseSessionId)
                    .ToList();

                var campaignApplies = selectedSessionIds
                    .Any(id => campaignSessionIds.Contains(id));

                if (!campaignApplies)
                    return response.SetError(
                        ErrorCodes.InvalidParameters,
                        "Campania nu se aplică sesiunilor selectate."
                    );
            }

            if (marketingCampaign != null)
            {
                contract.Discounts.Add(new ContractDiscount
                {
                    Type = marketingCampaign.DiscountType,
                    Value = marketingCampaign.DiscountValue,
                    Reason = $"Campanie marketing: {marketingCampaign.Name}",
                    Scope = marketingCampaign.DiscountScope,
                    MarketingCampaignId = marketingCampaign.Id
                });
            }
            else if (dto.Discounts != null)
            {
                foreach (var d in dto.Discounts)
                {
                    if (!Enum.TryParse<DiscountType>(d.Type, true, out var discountType))
                        return response.SetError(
                            ErrorCodes.InvalidParameters,
                            "Tipul discountului este invalid."
                        );

                    if (!Enum.TryParse<DiscountScope>(d.Scope, true, out var discountScope))
                        return response.SetError(
                            ErrorCodes.InvalidParameters,
                            "Scope-ul discountului este invalid."
                        );

                    contract.Discounts.Add(new ContractDiscount
                    {
                        Type = discountType,
                        Value = d.Value,
                        Reason = d.Reason,
                        Scope = discountScope
                    });
                }
            }

            foreach (var session in sessions)
            {
                var priceSnapshot = _pricingService.CalculateCourseSnapshotPrice(
                    session,
                    sessions,
                    contract
                );

                contract.Courses.Add(new ContractCourse
                {
                    CourseSessionId = session.Id,
                    CourseNameSnapshot = session.Course.Name,
                    SessionNameSnapshot = session.Title,
                    PriceSnapshot = priceSnapshot,
                    FeeType = session.FeeType
                });
            }

            var pricing = _pricingService.CalculatePricingFromContractCourses(contract);

            contract.TotalAmount = pricing.TotalAmount;
            contract.MonthlyAmount = pricing.MonthlyAmount;

            contract.InstallmentsList.Clear();

            foreach (var installment in _installmentService.BuildInstallments(
                         contract.StartDate,
                         contract.IsUnlimited,
                         contract.Installments,
                         pricing))
            {
                contract.InstallmentsList.Add(installment);
            }

            contract.ContractBody = await GenerateContractBody(
                contract,
                guardian,
                student);

            contract.IsBodyCustomized = false;

            await using var transaction = await _db.Database.BeginTransactionAsync();

            _db.StudentContracts.Add(contract);

            await _db.SaveChangesAsync();

            _activityLogService.Add(
                nameof(StudentContract),
                contract.Id.ToString(),
                "Create",
                $"Contractul {contract.ContractNumber} a fost creat.",
                GetCurrentUser()
            );

            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            await NotifyAdminsAsync(
                NotificationEvents.ContractActivity,
                "Contract nou creat",
                $"Contractul {contract.ContractNumber} a fost creat.",
                "Success",
                $"/contracts/{contract.Id}",
                nameof(StudentContract),
                contract.Id.ToString()
            );

            return response.SetCreated(new { contract.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateContract failed");

            return response.SetError(
                ErrorCodes.InternalServerError,
                "A apărut o eroare internă."
            );
        }
    }

    public async Task<PublicResponse> UpdateAsync(int id, UpdateContractDto dto)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .Include(c => c.Courses)
            .Include(c => c.Discounts)
            .Include(c => c.Parties)
                .ThenInclude(p => p.Guardian)
            .Include(c => c.Parties)
                .ThenInclude(p => p.Student)
            .Include(c => c.InstallmentsList)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Contractul nu a fost găsit.");

        if (contract.Status != ContractStatus.Draft)
            return response.SetError(ErrorCodes.InvalidParameters, "Doar contractele în draft pot fi modificate.");

        if (!dto.IsUnlimited && dto.EndDate == null)
            return response.SetError(ErrorCodes.InvalidParameters, "Data de sfârșit este obligatorie.");

        if (!dto.IsUnlimited && dto.EndDate < dto.StartDate)
            return response.SetError(ErrorCodes.InvalidParameters, "Perioada contractului este invalidă.");

        var student = contract.Parties
            .Where(p => p.Role == ContractPartyRole.Student)
            .Select(p => p.Student)
            .FirstOrDefault();

        if (student is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Studentul contractului nu a fost găsit.");

        var guardian = contract.Parties
            .Where(p => p.Role == ContractPartyRole.Guardian)
            .Select(p => p.Guardian)
            .FirstOrDefault();

        var existingSessionIds = contract.Courses
            .Select(c => c.CourseSessionId)
            .ToList();

        var sessions = await _db.CourseSessions
            .Include(x => x.Course)
            .Where(x => existingSessionIds.Contains(x.Id))
            .ToListAsync();

        if (sessions.Count != existingSessionIds.Count)
            return response.SetError(ErrorCodes.InvalidParameters, "Una sau mai multe sesiuni sunt invalide.");

        contract.StartDate = dto.StartDate;
        contract.EndDate = dto.IsUnlimited ? null : dto.EndDate;
        contract.IsUnlimited = dto.IsUnlimited;
        contract.Installments = dto.Installments <= 0 ? 1 : dto.Installments;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        ERPSystem.Data.Entities.MarketingCampaign? marketingCampaign = null;

        if (dto.MarketingCampaignId.HasValue)
        {
            marketingCampaign = await _db.MarketingCampaigns
                .Include(x => x.CourseSessions)
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.MarketingCampaignId.Value &&
                    x.IsActive &&
                    x.StartDate <= DateTime.Today &&
                    x.EndDate >= DateTime.Today);

            if (marketingCampaign is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Campania nu este validă sau nu mai este activă.");

            var selectedSessionIds = sessions.Select(x => x.Id).ToList();

            var campaignSessionIds = marketingCampaign.CourseSessions
                .Select(x => x.CourseSessionId)
                .ToList();

            var campaignApplies = selectedSessionIds.Any(id => campaignSessionIds.Contains(id));

            if (!campaignApplies)
                return response.SetError(ErrorCodes.InvalidParameters, "Campania nu se aplică sesiunilor acestui contract.");
        }

        contract.Discounts.Clear();

        if (marketingCampaign is not null)
        {
            contract.Discounts.Add(new ContractDiscount
            {
                Type = marketingCampaign.DiscountType,
                Value = marketingCampaign.DiscountValue,
                Reason = $"Campanie marketing: {marketingCampaign.Name}",
                Scope = marketingCampaign.DiscountScope,
                MarketingCampaignId = marketingCampaign.Id
            });
        }
        else if (dto.Discounts != null)
        {
            foreach (var d in dto.Discounts)
            {
                if (!Enum.TryParse<DiscountType>(d.Type, true, out var discountType))
                    return response.SetError(ErrorCodes.InvalidParameters, "Tipul discountului este invalid.");

                if (!Enum.TryParse<DiscountScope>(d.Scope, true, out var discountScope))
                    return response.SetError(ErrorCodes.InvalidParameters, "Scope-ul discountului este invalid.");

                contract.Discounts.Add(new ContractDiscount
                {
                    Type = discountType,
                    Value = d.Value,
                    Reason = d.Reason,
                    Scope = discountScope
                });
            }
        }

        foreach (var contractCourse in contract.Courses)
        {
            var session = sessions.FirstOrDefault(s => s.Id == contractCourse.CourseSessionId);

            if (session == null)
                return response.SetError(ErrorCodes.InvalidParameters, "Sesiunea cursului nu a fost găsită.");

            contractCourse.PriceSnapshot = _pricingService.CalculateCourseSnapshotPrice(
                session,
                sessions,
                contract
            );
        }

        var pricing = _pricingService.CalculatePricingFromContractCourses(contract);

        contract.TotalAmount = pricing.TotalAmount;
        contract.MonthlyAmount = pricing.MonthlyAmount;

        contract.InstallmentsList.Clear();

        foreach (var installment in _installmentService.BuildInstallments(
            contract.StartDate,
            contract.IsUnlimited,
            contract.Installments,
            pricing))
        {
            contract.InstallmentsList.Add(installment);
        }

        if (!contract.IsBodyCustomized)
        {
            contract.ContractBody = await GenerateContractBody(contract, guardian, student);
        }

        await _db.SaveChangesAsync();

        _activityLogService.Add(
             nameof(StudentContract),
             contract.Id.ToString(),
             "Update",
             $"Contractul {contract.ContractNumber} a fost actualizat.",
             GetCurrentUser()
         );

        await _db.SaveChangesAsync();

        return response.SetSuccess(new { contract.Id });
    }

    public async Task<PublicResponse> UpdateBodyAsync(int id, string body)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts.FindAsync(id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Contractul nu a fost găsit.");

        if (contract.Status != ContractStatus.Draft)
            return response.SetError(ErrorCodes.InvalidParameters, "Doar contractele în draft pot fi modificate.");

        if (string.IsNullOrWhiteSpace(body))
            return response.SetError(ErrorCodes.InvalidParameters, "Conținutul contractului nu poate fi gol.");

        contract.ContractBody = body;
        contract.IsBodyCustomized = true;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _activityLogService.Add(
             nameof(StudentContract),
             contract.Id.ToString(),
             "UpdateBody",
             $"Conținutul contractului {contract.ContractNumber} a fost actualizat.",
             GetCurrentUser()
         );

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> ResetBodyAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .Include(c => c.Parties)
                .ThenInclude(p => p.Guardian)
            .Include(c => c.Parties)
                .ThenInclude(p => p.Student)
            .Include(c => c.Courses)
            .Include(c => c.InstallmentsList)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Contractul nu a fost găsit.");

        if (contract.Status != ContractStatus.Draft)
            return response.SetError(ErrorCodes.InvalidParameters, "Doar contractele în draft pot fi modificate.");

        var student = contract.Parties
            .Where(p => p.Role == ContractPartyRole.Student)
            .Select(p => p.Student)
            .FirstOrDefault();

        if (student is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Studentul contractului nu a fost găsit.");

        var guardian = contract.Parties
            .Where(p => p.Role == ContractPartyRole.Guardian)
            .Select(p => p.Guardian)
            .FirstOrDefault();

        contract.ContractBody = await GenerateContractBody(contract, guardian, student);
        contract.IsBodyCustomized = false;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _activityLogService.Add(
            nameof(StudentContract),
            contract.Id.ToString(),
            "ResetBody",
            $"Conținutul contractului {contract.ContractNumber} a fost resetat la șablon.",
            GetCurrentUser()
        );

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> FinalizeAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .Include(c => c.Courses)
            .Include(c => c.InstallmentsList)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Contractul nu a fost găsit.");

        if (contract.Status != ContractStatus.Draft)
            return response.SetError(ErrorCodes.InvalidParameters, "Doar contractele în draft pot fi finalizate.");

        if (string.IsNullOrWhiteSpace(contract.ContractBody))
            return response.SetError(ErrorCodes.InvalidParameters, "Conținutul contractului este gol.");

        if (!contract.Courses.Any())
            return response.SetError(ErrorCodes.InvalidParameters, "Contractul nu are cursuri asociate.");

        if (!contract.InstallmentsList.Any())
            return response.SetError(ErrorCodes.InvalidParameters, "Contractul nu are rate generate.");

        contract.Status = ContractStatus.Finalized;
        contract.FinalizedAtUtc = DateTime.UtcNow;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        _activityLogService.Add(
            nameof(StudentContract),
            contract.Id.ToString(),
            "Finalize",
            $"Contractul {contract.ContractNumber} a fost finalizat.",
            GetCurrentUser()
        );

        await _db.SaveChangesAsync();

        await NotifyAdminsAsync(
            NotificationEvents.ContractActivity,
            "Contract finalizat",
            $"Contractul {contract.ContractNumber} a fost finalizat.",
            "Info",
            $"/contracts/{contract.Id}",
            nameof(StudentContract),
            contract.Id.ToString()
        );

        return response.SetSuccess(true);
    }

    public Task<PublicResponse> SendToClientAsync(SigningEntityType type, int id)
    {
        return _documentSigningService.SendToClientAsync(type, id);
    }

    public Task<PublicResponse> SignByClientAsync(string token, string signature)
    {
        return _documentSigningService.SignByClientAsync(token, signature);
    }

    public Task<PublicResponse> SignByAdminAsync(SigningEntityType type, int id, string signature)
    {
        return _documentSigningService.SignByAdminAsync(type, id, signature);
    }

    public Task<PublicResponse> GetContractForSigningAsync(string token)
    {
        return _documentSigningService.GetContractForSigningAsync(token);
    }
    
    public async Task<PublicResponse> CancelAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Contractul nu a fost găsit.");

        if (contract.Status == ContractStatus.Cancelled)
            return response.SetError(ErrorCodes.InvalidParameters, "Contractul este deja anulat.");

        if (contract.Status == ContractStatus.Completed ||
            contract.Status == ContractStatus.Expired)
            return response.SetError(ErrorCodes.InvalidParameters, "Contractul nu poate fi anulat.");

        var acts = await _db.ContractAdditionalAct
           .Where(a => a.ContractId == contract.Id)
           .ToListAsync();

        contract.Status = ContractStatus.Cancelled;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var act in acts)
        {
            act.Status = AdditionalActStatus.Cancelled;
        }

        var today = DateTime.UtcNow.Date;
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);

        var installments = await _db.ContractInstallments
           .Where(i =>
               i.ContractId == contract.Id &&
               i.PaidAmount < i.Amount &&
               i.DueDate >= tomorrow)
           .ToListAsync();

        foreach (var i in installments)
        {
            i.Status = InstallmentStatus.Cancelled;
        }

        _activityLogService.Add(
             nameof(StudentContract),
             contract.Id.ToString(),
             "Cancelled",
             $"Contractl {contract.ContractNumber} a fost anulat",
             GetCurrentUser()
         );
         
        var enrollments = await _db.CourseEnrollments
            .Include(e => e.Student)
            .Include(e => e.Session)
            .Where(e => e.ContractId == contract.Id && e.IsActive)
            .ToListAsync();

        foreach (var e in enrollments)
        {
            e.IsActive = false;
            e.ContractId = null;
            e.EndedAtUtc = DateTime.UtcNow;

            _activityLogService.Add(
                nameof(CourseEnrollment),
                e.Id.ToString(),
                "EnrollmentCancelled",
                $"Cursantul {e.Student.FirstName} {e.Student.LastName} a fost eliminat din {e.Session.Title} deoarece contractul a fost anulat.",
                GetCurrentUser()
            );
        }

        await _db.SaveChangesAsync();

        await NotifyAdminsAsync(
                NotificationEvents.ContractActivity,
                "Contract anulat",
                $"Contractul {contract.ContractNumber} a fost anulat.",
                "Warning",
                $"/contracts/{contract.Id}",
                nameof(StudentContract),
                contract.Id.ToString()
            );

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> CompleteAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        var acts = await _db.ContractAdditionalAct
           .Where(a => a.ContractId == contract.Id &&
                       a.Status != AdditionalActStatus.Cancelled)
           .ToListAsync();

        if (contract.Status != ContractStatus.Active)
            return response.SetError(ErrorCodes.InvalidParameters,
                "Doar contractele active pot fi finalizate.");

        contract.Status = ContractStatus.Completed;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var act in acts)
        {
            if (act.Status == AdditionalActStatus.Draft ||
                act.Status == AdditionalActStatus.Finalized ||
                act.Status == AdditionalActStatus.SentToClient ||
                act.Status == AdditionalActStatus.SignedByClient)
            {
                act.Status = AdditionalActStatus.Cancelled;
            }

        }

        var today = DateTime.UtcNow.Date;
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);

        var installments = await _db.ContractInstallments
           .Where(i =>
               i.ContractId == contract.Id &&
               i.PaidAmount < i.Amount &&
               i.DueDate >= tomorrow)
           .ToListAsync();

        foreach (var i in installments)
        {
            if (!i.IsPaid)
                i.Status = InstallmentStatus.Cancelled;
        }

        _activityLogService.Add(
            nameof(StudentContract),
            contract.Id.ToString(),
            "Completed",
            $"Contractul {contract.ContractNumber} a fost finalizat.",
            GetCurrentUser()
        );

        var enrollments = await _db.CourseEnrollments
            .Include(e => e.Student)
            .Include(e => e.Session)
            .Where(e => e.ContractId == contract.Id && e.IsActive)
            .ToListAsync();

        foreach (var e in enrollments)
        {
            e.IsActive = false;
            e.ContractId = null;
            e.EndedAtUtc = contract.EndDate ?? DateTime.UtcNow;

            _activityLogService.Add(
                nameof(CourseEnrollment),
                e.Id.ToString(),
                "EnrollmentCompleted",
               $"Cursantul {e.Student.FirstName} {e.Student.LastName} a finalizat sesiunea {e.Session.Title}.",
                GetCurrentUser()
            );
        }

        await _db.SaveChangesAsync();

        await NotifyAdminsAsync(
               NotificationEvents.ContractActivity,
               "Contract finalizat",
               $"Contractul {contract.ContractNumber} a fost finalizat.",
               "Success",
               $"/contracts/{contract.Id}",
               nameof(StudentContract),
               contract.Id.ToString()
           );

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> DeleteAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .Include(c => c.InstallmentsList)
            .Include(c => c.Discounts)
            .Include(c => c.Courses)
            .Include(c => c.Parties)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Contractul nu a fost găsit.");

        if (contract.Status != ContractStatus.Draft)
            return response.SetError(ErrorCodes.InvalidParameters, "Doar contractele în stare Draft pot fi șterse.");

        _db.StudentContracts.Remove(contract);

        _activityLogService.Add(
            nameof(StudentContract),
            contract.Id.ToString(),
            "Delete",
            $"Contractul {contract.ContractNumber} a fost șters.",
            GetCurrentUser()
        );

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    public async Task ExpireContractsAsync()
    {
        var contracts = await _db.StudentContracts
            .Where(c =>
                c.Status == ContractStatus.Active &&
                !c.IsUnlimited &&
                c.EndDate < DateTime.UtcNow)
            .ToListAsync();


        foreach (var contract in contracts)
        {
            contract.Status = ContractStatus.Expired;
            contract.UpdatedAtUtc = DateTime.UtcNow;
            var acts = await _db.ContractAdditionalAct
           .Where(a => a.ContractId == contract.Id &&
                       a.Status == AdditionalActStatus.Applied)
           .ToListAsync();
            foreach (var act in acts)
            {
                act.Status = AdditionalActStatus.Expired;
            }

            _activityLogService.Add(
               nameof(StudentContract),
               contract.Id.ToString(),
               "Expired",
               $"Contract {contract.ContractNumber} a expirat",
               GetCurrentUser()
             );
             
            var installments = await _db.ContractInstallments
              .Where(i => i.ContractId == contract.Id &&!i.IsPaid &&  i.DueDate.Date > DateTime.UtcNow.Date)
              .ToListAsync();

            foreach (var i in installments)
            {
                if (!i.IsPaid)
                    i.Status = InstallmentStatus.Expired;
            }

            var enrollments = await _db.CourseEnrollments
                .Include(e => e.Student)
                .Include(e => e.Session)
                .Where(e => e.ContractId == contract.Id && e.IsActive)
                .ToListAsync();

            foreach (var e in enrollments)
            {
                e.IsActive = false;
                e.EndedAtUtc = DateTime.UtcNow;

                _activityLogService.Add(
                     nameof(CourseEnrollment),
                     e.Id.ToString(),
                     "EnrollmentEnded",
                     $"Student {e.Student.FirstName} {e.Student.LastName} a fost scos din sesiunea {e.Session.Title} (contract expirat)",
                     GetCurrentUser()
                 );

            }

            await NotifyAdminsAsync(
                 NotificationEvents.ContractActivity,
                 "Contract expirat",
                 $"Contractul {contract.ContractNumber} a expirat.",
                 "Warning",
                 $"/contracts/{contract.Id}",
                 nameof(StudentContract),
                 contract.Id.ToString()
             );
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IResult> DownloadContractAsync(int id)
    {
        var contract = await _db.StudentContracts.FindAsync(id);

        if (contract == null)
            return Results.NotFound();

        if (string.IsNullOrEmpty(contract.PdfPath))
            return Results.BadRequest("PDF-ul nu a fost generat.");

        var filePath = Path.Combine("wwwroot", "contracts", contract.PdfPath);

        if (!File.Exists(filePath))
        {
            var fileName = _pdfService.GenerateContractPdf(contract);

            contract.PdfPath = fileName;
            await _db.SaveChangesAsync();

            filePath = Path.Combine("wwwroot", "contracts", fileName);
        }



        var bytes = await File.ReadAllBytesAsync(filePath);

        return Results.File(
            bytes,
            "application/pdf",
            $"Contract_{contract.ContractNumber}.pdf"
        );


    }

    public async Task<PublicResponse> ListByStudentAsync(int studentId)
    {
        var response = new PublicResponse(true);

        var contracts = await _db.StudentContracts
            .AsNoTracking()
            .Include(c => c.Parties)
                .ThenInclude(p => p.Guardian)
            .Include(c => c.InstallmentsList) 
            .Where(c => c.Parties.Any(p =>
                p.StudentId == studentId &&
                p.Role == ContractPartyRole.Student))
            .Select(c => new ContractListItemDto
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,

                GuardianName = c.Parties
                    .Where(p => p.Role == ContractPartyRole.Guardian)
                    .Select(p => p.Guardian.FirstName + " " + p.Guardian.LastName)
                    .FirstOrDefault(),

                StartDate = c.StartDate,
                EndDate = c.EndDate,

                TotalAmount = c.TotalAmount,

               
                DisplayTotal =
                    c.TotalAmount.HasValue
                        ? c.TotalAmount.Value.ToString("0.##") + " RON"
                        : "Abonament lunar",

                IsUnlimited = c.IsUnlimited,

                
                MonthlyAmount = c.InstallmentsList
                    .OrderBy(i => i.DueDate)
                    .Select(i => i.Amount)
                    .FirstOrDefault(),

                Status = c.Status.ToString(),
                CreatedAtUtc = c.CreatedAtUtc
            })
            .ToListAsync();

        return response.SetSuccess(contracts);
    }

    public async Task<PublicResponse> GetByIdAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .AsNoTracking()
            .Include(c => c.Courses)
            .Include(c => c.Discounts)
            .Include(c => c.InstallmentsList) 
            .Include(c => c.Parties)
                .ThenInclude(p => p.Student)
            .Include(c => c.Parties)
                .ThenInclude(p => p.Guardian)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        var monthlyAmount = contract.TotalAmount == null
            ? contract.InstallmentsList.FirstOrDefault()?.Amount ?? 0
            : 0;

        var displayTotal = contract.TotalAmount.HasValue
            ? $"{contract.TotalAmount.Value:F2} RON"
            : $"{monthlyAmount:F2} RON / lună";



        var dto = new ContractDetailsDto
        {
            Id = contract.Id,
            StudentId = contract.Parties
              .Where(p => p.StudentId.HasValue)
              .Select(p => p.StudentId)
              .FirstOrDefault(),
            ContractNumber = contract.ContractNumber,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            IsUnlimited = contract.IsUnlimited,
            TotalAmount = contract.TotalAmount,

            DisplayTotal = displayTotal,
            MonthlyAmount = monthlyAmount,

            Installments = contract.Installments,
            Status = contract.Status.ToString(),
            CreatedAtUtc = contract.CreatedAtUtc,
            FinalizedAtUtc = contract.FinalizedAtUtc,

            // semnături
            ClientSignature = contract.ClientSignature,
            ClientSignedAtUtc = contract.ClientSignedAtUtc,
            AdminSignature = contract.AdminSignature,
            AdminSignedAtUtc = contract.AdminSignedAtUtc,

            // company
            CompanyName = contract.CompanyNameSnapshot,
            CompanyAddress = contract.CompanyAddressSnapshot,
            CompanyCui = contract.CompanyCuiSnapshot,
            CompanyRegistration = contract.CompanyRegistrationSnapshot,
            CompanyIban = contract.CompanyIbanSnapshot,
            CompanyBank = contract.CompanyBankSnapshot,
            CompanyEmail = contract.CompanyEmailSnapshot,
            CompanyPhone = contract.CompanyPhoneSnapshot,

            // beneficiar
            BeneficiaryName = contract.BeneficiaryNameSnapshot,
            BeneficiaryEmail = contract.BeneficiaryEmailSnapshot,
            BeneficiaryPhone = contract.BeneficiaryPhoneSnapshot,
            BeneficiaryAddress = contract.BeneficiaryAddressSnapshot,

            ContractBody = contract.ContractBody,

            // parties
            Parties = contract.Parties
         .Select(p => new ContractPartyDto
         {
             StudentId = p.StudentId,
             StudentName = p.Student != null
                 ? p.Student.FirstName + " " + p.Student.LastName
                 : null,
             GuardianId = p.GuardianId,
             GuardianName = p.Guardian != null
                 ? p.Guardian.FirstName + " " + p.Guardian.LastName
                 : null,
             Role = p.Role.ToString()
         })
         .ToList(),

            // courses
            Courses = contract.Courses
         .Select(c => new ContractCourseDto
         {
             CourseSessionId = c.CourseSessionId,
             CourseName = c.CourseNameSnapshot,
             SessionName = c.SessionNameSnapshot,
             Price = c.PriceSnapshot,
             CourseFeeType = (int)c.FeeType
         })
         .ToList(),

            // discounts
            Discounts = contract.Discounts
         .Select(d => new ContractDiscountDto
         {
             Type = d.Type.ToString(),
             Value = d.Value,
             Reason = d.Reason,
             Scope = d.Scope.ToString()
         })
         .ToList(),

            // installments
            InstallmentsList = contract.InstallmentsList
         .OrderBy(i => i.DueDate)
         .Select(i => new InstallmentDto
         {
             Id = i.Id,
             DueDate = i.DueDate,
             Amount = i.Amount,
             PaidAmount = i.PaidAmount
         })
         .ToList()
        };

        return response.SetSuccess(dto);
    }

    private string GenerateContractNumber()
    {
        return $"CTR-{DateTime.UtcNow:yyyy}-{DateTime.UtcNow.Ticks.ToString()[^6..]}";
    }

    private async Task<string> GenerateContractBody( StudentContract contract, Guardian? guardian, Data.Entities.Student student)
    {
        var template = await _db.ContractTemplates
            .Where(x => x.IsActive && x.Name == "Default ERP Contract Template")
            .Select(x => x.Body)
            .FirstAsync();

        var hasPackage = contract.Courses.Any(c => c.FeeType == CourseFeeType.FixedPackage);
        var hasMonthly = contract.Courses.Any(c => c.FeeType == CourseFeeType.Monthly);

        
        var sessionIds = contract.Courses.Select(c => c.CourseSessionId).ToList();
        var originalFees = await _db.CourseSessions
            .Where(s => sessionIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Fee);

        var packageBaseAmount = contract.Courses
            .Where(c => c.FeeType == CourseFeeType.FixedPackage)
            .Sum(c => originalFees.GetValueOrDefault(c.CourseSessionId, c.PriceSnapshot));

        var monthlyBaseAmount = contract.Courses
            .Where(c => c.FeeType == CourseFeeType.Monthly)
            .Sum(c => originalFees.GetValueOrDefault(c.CourseSessionId, c.PriceSnapshot));

        var subtotal = packageBaseAmount + monthlyBaseAmount;

        var contractType =
            hasPackage && hasMonthly
                ? "pachet fix și abonament lunar"
                : hasPackage
                    ? "pachet fix"
                    : "abonament lunar";

        var studentList = $"<li>{student.FullName}</li>";

        var coursesList = string.Join("",
            contract.Courses.Select(c =>
            {
                var feeTypeText = c.FeeType == CourseFeeType.FixedPackage
                    ? "pachet fix"
                    : "abonament lunar";

                return $"<li>{c.CourseNameSnapshot} ({c.SessionNameSnapshot}) – {c.PriceSnapshot:F2} RON ({feeTypeText})</li>";
            }));

        var pricing = new PricingResult
        {
            PackageAmount = packageBaseAmount,
            MonthlyAmount = monthlyBaseAmount,
            Months = !contract.IsUnlimited && contract.EndDate.HasValue
                ? ((contract.EndDate.Value.Year - contract.StartDate.Year) * 12)
                  + contract.EndDate.Value.Month
                  - contract.StartDate.Month
                  + 1
                : 0
        };

        foreach (var discount in contract.Discounts)
        {
            ApplyTemplateDiscount(pricing, discount, contract.IsUnlimited);
        }

        if (contract.IsUnlimited)
        {
            pricing.TotalAmount = hasPackage
                ? pricing.PackageAmount
                : null;
        }
        else
        {
            pricing.TotalAmount =
                pricing.PackageAmount + pricing.MonthlyAmount * pricing.Months;
        }

        var period = contract.IsUnlimited
            ? "Perioadă nedeterminată"
            : $"{contract.StartDate:dd.MM.yyyy} - {contract.EndDate:dd.MM.yyyy}";

        var totalDisplay = pricing.TotalAmount.HasValue
            ? $"{pricing.TotalAmount.Value:F2} RON"
            : "Contract cu plată recurentă lunară";

        var packageDisplay = hasPackage
            ? $"{pricing.PackageAmount:F2} RON"
            : "-";

        var monthlyDisplay = hasMonthly
            ? $"{pricing.MonthlyAmount:F2} RON / lună"
            : "-";

        var discountDisplay = pricing.DiscountTotal > 0
            ? $"{pricing.DiscountTotal:F2} RON"
            : "-";

        var installmentsDisplay = hasPackage
            ? contract.Installments.ToString()
            : "-";
      
        var totalLabel = contract.IsUnlimited
            ? "Total pachet fix"
            : "Total estimat contract";

        var paymentPlan = BuildPaymentPlanText(contract);

        var values = new Dictionary<string, string>
        {
            ["ContractNumber"] = contract.ContractNumber,
            ["Date"] = DateTime.UtcNow.ToString("dd.MM.yyyy"),

            ["CompanyName"] = contract.CompanyNameSnapshot,
            ["CompanyCui"] = contract.CompanyCuiSnapshot,
            ["CompanyRegistration"] = contract.CompanyRegistrationSnapshot,
            ["CompanyAddress"] = contract.CompanyAddressSnapshot,
            ["CompanyIban"] = contract.CompanyIbanSnapshot,
            ["CompanyBank"] = contract.CompanyBankSnapshot,
            ["CompanyEmail"] = contract.CompanyEmailSnapshot,
            ["CompanyPhone"] = contract.CompanyPhoneSnapshot,

            ["BeneficiaryName"] = contract.BeneficiaryNameSnapshot,
            ["BeneficiaryEmail"] = contract.BeneficiaryEmailSnapshot,
            ["BeneficiaryPhone"] = contract.BeneficiaryPhoneSnapshot,
            ["BeneficiaryAddress"] = contract.BeneficiaryAddressSnapshot,

            ["Students"] = studentList,
            ["Courses"] = coursesList,

            ["ContractPeriod"] = period,

            ["ContractType"] = contractType,
            ["Subtotal"] = $"{subtotal:F2} RON",
            ["PackageAmount"] = packageDisplay,
            ["MonthlyAmount"] = monthlyDisplay,
            ["Discount"] = discountDisplay,
            ["TotalLabel"] = totalLabel,
            ["Total"] = totalDisplay,
            ["Installments"] = installmentsDisplay,
            ["PaymentPlan"] = paymentPlan
        };

        return _templateRenderer.Render(template, values);
    }

    private static void ApplyTemplateDiscount( PricingResult result, ContractDiscount discount,   bool isUnlimited)
    {
        switch (discount.Scope)
        {
            case DiscountScope.Package:
                result.PackageAmount = ApplyTemplateDiscountValue(
                    result.PackageAmount,
                    discount,
                    result);
                break;

            case DiscountScope.Subscription:
                result.MonthlyAmount = ApplyTemplateDiscountValue(
                    result.MonthlyAmount,
                    discount,
                    result);
                break;

            case DiscountScope.Total:
                if (isUnlimited)
                {
                    if (discount.Type == DiscountType.Percentage)
                    {
                        if (result.PackageAmount > 0)
                            result.PackageAmount = ApplyTemplateDiscountValue(result.PackageAmount, discount, result);

                        if (result.MonthlyAmount > 0)
                            result.MonthlyAmount = ApplyTemplateDiscountValue(result.MonthlyAmount, discount, result);
                    }
                    else
                    {
                       
                        var unlimitedTotal = result.PackageAmount + result.MonthlyAmount;
                        if (unlimitedTotal > 0)
                        {
                            var pkgShare = discount.Value * result.PackageAmount / unlimitedTotal;
                            var subShare = discount.Value * result.MonthlyAmount / unlimitedTotal;

                            var oldPkg = result.PackageAmount;
                            var oldSub = result.MonthlyAmount;

                            result.PackageAmount = Math.Max(0, Math.Round(result.PackageAmount - pkgShare, 2));
                            result.MonthlyAmount = Math.Max(0, Math.Round(result.MonthlyAmount - subShare, 2));

                            result.DiscountTotal += (oldPkg - result.PackageAmount) + (oldSub - result.MonthlyAmount);
                        }
                    }
                }
                else
                {
                    var total = result.PackageAmount + result.MonthlyAmount * result.Months;

                    var discountedTotal = ApplyTemplateDiscountValue(
                        total,
                        discount,
                        result);

                    if (total > 0)
                    {
                        var ratio = discountedTotal / total;

                        result.PackageAmount = Math.Round(result.PackageAmount * ratio, 2);
                        result.MonthlyAmount = Math.Round(result.MonthlyAmount * ratio, 2);
                    }
                }

                break;
        }
    }

    private static decimal ApplyTemplateDiscountValue(  decimal amount,  ContractDiscount discount,  PricingResult result)
    {
        if (amount <= 0 || discount.Value <= 0)
            return amount;

        var discounted = discount.Type == DiscountType.Percentage
            ? amount - amount * (discount.Value / 100m)
            : amount - discount.Value;

        var safeDiscounted = Math.Max(0, Math.Round(discounted, 2));
        var diff = amount - safeDiscounted;

        if (diff > 0)
            result.DiscountTotal += diff;

        return safeDiscounted;
    }

    private static string BuildPaymentPlanText(StudentContract contract)
    {
        var installments = contract.InstallmentsList
            .OrderBy(x => x.DueDate)
            .ToList();

        if (!installments.Any())
            return "";

        var lines = installments.Select((i, index) =>
            $"Rata {index + 1}: {i.Amount:F2} RON, scadentă la {i.DueDate:dd.MM.yyyy}");

        return string.Join("<br>", lines);
    }

    public async Task NotifyAdminsAsync( string eventType, string title, string message, string type, string link, string entityType, string entityId)
    {
        await _notificationsService.CreateNotificationForRolesAsync(
            roleNames: new[] { "Admin", "Secretary" },
            eventType: eventType,
            title: title,
            message: message,
            type: type,
            link: link,
            entityType: entityType,
            entityId: entityId
            );
    }

    public async Task<PublicResponse> GetContractsOverviewAsync()
    {
        PublicResponse response = new(true);

        var contracts = await _db.StudentContracts
            .Include(x => x.Courses)
            .Include(x => x.InstallmentsList)
            .Include(x => x.AdditionalActs)
                .ThenInclude(x => x.Items)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ContractOverviewDto
            {
                Id = x.Id,
                ContractNumber = x.ContractNumber,

                BeneficiaryName = x.BeneficiaryNameSnapshot,
                BeneficiaryEmail = x.BeneficiaryEmailSnapshot,

                StartDate = x.StartDate,
                EndDate = x.EndDate,

                TotalAmount = x.TotalAmount,
                MonthlyAmount = x.MonthlyAmount,

                Status = x.Status.ToString(),

                CoursesCount = x.Courses.Count,
                InstallmentsCount = x.InstallmentsList.Count,
                PaidInstallmentsCount = x.InstallmentsList.Count(i => i.PaidAmount >= i.Amount),

                AdditionalActs = x.AdditionalActs
                    .OrderByDescending(a => a.CreatedAtUtc)
                    .Select(a => new AdditionalActOverviewDto
                    {
                        Id = a.Id,
                        ActNumber = a.ActNumber,
                        Description = a.Description,
                        Status = a.Status.ToString(),
                        CreatedAtUtc = a.CreatedAtUtc,
                        ItemsCount = a.Items.Count
                    })
                    .ToList()
            })
            .ToListAsync();

        return response.SetSuccess(contracts);
    }

    public async Task<IResult> ExportContractsExcelAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _db.StudentContracts
            .Include(x => x.Courses)
            .Include(x => x.Discounts)
            .Include(x => x.InstallmentsList)
            .Include(x => x.AdditionalActs)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(x => x.CreatedAtUtc.Date >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(x => x.CreatedAtUtc.Date <= to.Value.Date);

        var contracts = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        using var workbook = new XLWorkbook();

        var contractsSheet = workbook.Worksheets.Add("Contracte");
        var installmentsSheet = workbook.Worksheets.Add("Rate");

       

        var contractHeaders = new[]
        {
        "Nr. contract",
        "Beneficiar",
        "Email",
        "Telefon",
        "Status",
        "Data început",
        "Data sfârșit",
        "Nelimitat",
        "Total contract",
        "Sumă lunară",
        "Nr. rate",
        "Rate plătite",
        "Total rate",
        "Total plătit",
        "Rest de plată",
        "Discounturi",
        "Cursuri",
        "Acte adiționale",
        "Creat la",
        "Finalizat la",
        "Activat la"
    };

        for (int i = 0; i < contractHeaders.Length; i++)
            contractsSheet.Cell(1, i + 1).Value = contractHeaders[i];

        var contractRow = 2;

        foreach (var c in contracts)
        {
            var installments = c.InstallmentsList.ToList();

            var totalInstallmentsAmount = installments.Sum(i => i.Amount);
            var totalPaid = installments.Sum(i => i.PaidAmount);
            var remaining = totalInstallmentsAmount - totalPaid;
            var paidInstallmentsCount = installments.Count(i => i.PaidAmount >= i.Amount);

            var courses = c.Courses.Any()
                ? string.Join(" | ", c.Courses.Select(x =>
                    $"{x.CourseNameSnapshot} - {x.SessionNameSnapshot} ({x.PriceSnapshot:0.00})"))
                : "";

            var discounts = c.Discounts.Any()
                ? string.Join(" | ", c.Discounts.Select(x =>
                    $"{x.Type}: {x.Value:0.00} - {x.Reason}"))
                : "";

            contractsSheet.Cell(contractRow, 1).Value = c.ContractNumber;
            contractsSheet.Cell(contractRow, 2).Value = c.BeneficiaryNameSnapshot;
            contractsSheet.Cell(contractRow, 3).Value = c.BeneficiaryEmailSnapshot;
            contractsSheet.Cell(contractRow, 4).Value = c.BeneficiaryPhoneSnapshot;
            contractsSheet.Cell(contractRow, 5).Value = c.Status.ToString();
            contractsSheet.Cell(contractRow, 6).Value = c.StartDate;
            contractsSheet.Cell(contractRow, 7).Value = c.EndDate;
            contractsSheet.Cell(contractRow, 8).Value = c.IsUnlimited ? "Da" : "Nu";
            contractsSheet.Cell(contractRow, 9).Value = c.TotalAmount ?? 0;
            contractsSheet.Cell(contractRow, 10).Value = c.MonthlyAmount;
            contractsSheet.Cell(contractRow, 11).Value = c.InstallmentsList.Count;
            contractsSheet.Cell(contractRow, 12).Value = paidInstallmentsCount;
            contractsSheet.Cell(contractRow, 13).Value = totalInstallmentsAmount;
            contractsSheet.Cell(contractRow, 14).Value = totalPaid;
            contractsSheet.Cell(contractRow, 15).Value = remaining;
            contractsSheet.Cell(contractRow, 16).Value = discounts;
            contractsSheet.Cell(contractRow, 17).Value = courses;
            contractsSheet.Cell(contractRow, 18).Value = c.AdditionalActs.Count;
            contractsSheet.Cell(contractRow, 19).Value = c.CreatedAtUtc;
            contractsSheet.Cell(contractRow, 20).Value = c.FinalizedAtUtc;
            contractsSheet.Cell(contractRow, 21).Value = c.ActivatedAtUtc;

            contractRow++;
        }

       
        var installmentHeaders = new[]
        {
        "Nr. contract",
        "Beneficiar",
        "Email",
        "Status contract",
        "Data scadență",
        "Sumă rată",
        "Sumă plătită",
        "Rest rată",
        "Status rată",
        "Este plătită",
        "Este restantă"
    };

        for (int i = 0; i < installmentHeaders.Length; i++)
            installmentsSheet.Cell(1, i + 1).Value = installmentHeaders[i];

        var installmentRow = 2;

        foreach (var c in contracts)
        {
            var installments = c.InstallmentsList.AsEnumerable();

            if (from.HasValue)
                installments = installments.Where(i => i.DueDate.Date >= from.Value.Date);

            if (to.HasValue)
                installments = installments.Where(i => i.DueDate.Date <= to.Value.Date);

            foreach (var i in installments.OrderBy(x => x.DueDate))
            {
                var remaining = i.Amount - i.PaidAmount;
                var isOverdue = i.PaidAmount < i.Amount && i.DueDate.Date < DateTime.UtcNow.Date;

                installmentsSheet.Cell(installmentRow, 1).Value = c.ContractNumber;
                installmentsSheet.Cell(installmentRow, 2).Value = c.BeneficiaryNameSnapshot;
                installmentsSheet.Cell(installmentRow, 3).Value = c.BeneficiaryEmailSnapshot;
                installmentsSheet.Cell(installmentRow, 4).Value = c.Status.ToString();
                installmentsSheet.Cell(installmentRow, 5).Value = i.DueDate;
                installmentsSheet.Cell(installmentRow, 6).Value = i.Amount;
                installmentsSheet.Cell(installmentRow, 7).Value = i.PaidAmount;
                installmentsSheet.Cell(installmentRow, 8).Value = remaining;
                installmentsSheet.Cell(installmentRow, 9).Value = i.Status.ToString();
                installmentsSheet.Cell(installmentRow, 10).Value = i.PaidAmount >= i.Amount ? "Da" : "Nu";
                installmentsSheet.Cell(installmentRow, 11).Value = isOverdue ? "Da" : "Nu";

                installmentRow++;
            }
        }

        _excelExportService.FormatSheet(contractsSheet);
        _excelExportService.FormatSheet(installmentsSheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var bytes = stream.ToArray();

        return Results.File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"contracte_contabilitate_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx"
        );
    }

    private string GetCurrentUser()
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? _httpContextAccessor.HttpContext?.User?
                .FindFirst("email")?.Value
            ?? _httpContextAccessor.HttpContext?.User?
                .FindFirst("username")?.Value
            ?? "system";
    }

}