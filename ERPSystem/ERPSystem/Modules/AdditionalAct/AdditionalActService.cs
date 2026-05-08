using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.AdditionalAct.Models;
using ERPSystem.Modules.Contracts;
using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Shared.BusinessLogic;
using ERPSystem.Shared.DTOs.PDF;
using ERPSystem.Shared.Notifications;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Enums;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Modules.AdditionalAct
{
    public class AdditionalActService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ContractsService> _logger;
        private readonly PdfService _pdfService;
        private readonly NotificationsService _notificationsService;
        private readonly TemplateRendererService _templateRenderer;
        private readonly ActivityLogService _activityLogService;
        private readonly ContractPricingService _pricingService;
        private readonly ContractInstallmentService _installmentService;

        public AdditionalActService(
            ApplicationDbContext db, 
            ILogger<ContractsService> logger, 
            PdfService pdfService, 
            NotificationsService notificationsService, 
            TemplateRendererService templateRenderer,
            ActivityLogService activityLogService,
            ContractPricingService pricingService,
            ContractInstallmentService installmentService)
        {
            _db = db;
            _logger = logger;
            _pdfService = pdfService;
            _notificationsService = notificationsService;
            _templateRenderer = templateRenderer;
            _activityLogService = activityLogService;
            _pricingService = pricingService;
            _installmentService = installmentService;
        }

        public async Task<PublicResponse> CreateAdditionalActAsync(int contractId, CreateAdditionalActDto dto)
        {
            var response = new PublicResponse(true);

            var contract = await _db.StudentContracts
                .Include(c => c.Parties)
                    .ThenInclude(p => p.Guardian)
                .Include(c => c.Parties)
                    .ThenInclude(p => p.Student)
                .Include(c => c.Courses)
                .Include(c => c.InstallmentsList)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
                return response.SetError(ErrorCodes.InvalidParameters, "Contract not found");

            if (contract.Status != ContractStatus.Active)
                return response.SetError(ErrorCodes.InvalidParameters, "Only active contracts can have additional acts");

            if (dto.Types == null || !dto.Types.Any())
                return response.SetError(ErrorCodes.InvalidParameters, "At least one type required");

            var act = new ContractAdditionalAct
            {
                ContractId = contract.Id,
                ActNumber = GenerateActNumber(),
                Status = AdditionalActStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow,
                Items = new List<ContractAdditionalActItem>()
            };

            try
            {
                var buildResult = await BuildAdditionalActItemsAsync(contract, dto);

                foreach (var item in buildResult.Items)
                {
                    act.Items.Add(item);
                }

                act.Description = buildResult.Description;
            }
            catch (InvalidOperationException ex)
            {
                return response.SetError(ErrorCodes.InvalidParameters, ex.Message);
            }

            var studentIds = contract.Parties
                .Where(p => p.StudentId != null)
                .Select(p => p.StudentId.Value)
                .ToList();

            var students = await _db.Students
                .Where(s => studentIds.Contains(s.Id))
                .ToListAsync();

            Guardian? guardian = null;

            if (students.Any(s => s.IsMinor))
            {
                guardian = contract.Parties
                    .FirstOrDefault(p => p.GuardianId != null)
                    ?.Guardian;
            }

            act.Body = await GenerateAdditionalActBody(contract, act, guardian, students);

            _db.ContractAdditionalAct.Add(act);

            await _db.SaveChangesAsync();

            _activityLogService.Add(
                nameof(ContractAdditionalAct),
                act.Id.ToString(),
                "Create",
                $"Actul adițional {act.ActNumber} a fost creat pentru contractul {contract.ContractNumber}. {act.Description}"
            );

            await _db.SaveChangesAsync();

            await _notificationsService.CreateNotificationForRolesAsync(
                roleNames: new[] { "Admin", "Secretary" },
                eventType: NotificationEvents.ContractActivity,
                title: "Act adițional nou",
                message: $"Actul adițional {act.ActNumber} a fost creat pentru contractul {contract.ContractNumber}.",
                type: "Success",
                link: $"/contracts/{contract.Id}/additional-acts/{act.Id}",
                entityType: nameof(ContractAdditionalAct),
                entityId: act.Id.ToString()
            );

            return response.SetSuccess(new { act.Id });
        }

        public async Task<PublicResponse> UpdateAdditionalActBodyAsync(int id, UpdateAdditionalActBodyDto dto)
        {
            var response = new PublicResponse(true);

            var act = await _db.ContractAdditionalAct
                .FirstOrDefaultAsync(a => a.Id == id);

            if (act == null)
                return response.SetError(ErrorCodes.InvalidParameters, "Act not found");

            if (act.Status != AdditionalActStatus.Draft)
                return response.SetError(ErrorCodes.InvalidParameters, "Only draft editable");

            if (string.IsNullOrWhiteSpace(dto.Body))
                return response.SetError(ErrorCodes.InvalidParameters, "Body is required");

            act.Body = dto.Body;

            act.PdfPath = null;

            await _db.SaveChangesAsync();

            return response.SetSuccess(new { act.Id });
        }

        public async Task<PublicResponse> UpdateAdditionalActAsync(int id, CreateAdditionalActDto dto)
        {
            var response = new PublicResponse(true);

            var act = await _db.ContractAdditionalAct
                .Include(a => a.Items)
                .Include(a => a.Contract)
                    .ThenInclude(c => c.Parties)
                        .ThenInclude(p => p.Guardian)
                .Include(a => a.Contract)
                    .ThenInclude(c => c.Parties)
                        .ThenInclude(p => p.Student)
                .Include(a => a.Contract)
                    .ThenInclude(c => c.Courses)
                .Include(a => a.Contract)
                    .ThenInclude(c => c.InstallmentsList)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (act == null)
                return response.SetError(ErrorCodes.InvalidParameters, "Act not found");

            if (act.Status != AdditionalActStatus.Draft)
                return response.SetError(ErrorCodes.InvalidParameters, "Only draft editable");

            var contract = act.Contract;

            act.Items.Clear();

            try
            {
                var buildResult = await BuildAdditionalActItemsAsync(contract, dto, act.Id);

                foreach (var item in buildResult.Items)
                {
                    act.Items.Add(item);
                }

                act.Description = buildResult.Description;
            }
            catch (InvalidOperationException ex)
            {
                return response.SetError(ErrorCodes.InvalidParameters, ex.Message);
            }

            var studentIds = contract.Parties
                .Where(p => p.StudentId != null)
                .Select(p => p.StudentId.Value)
                .ToList();

            var students = await _db.Students
                .Where(s => studentIds.Contains(s.Id))
                .ToListAsync();

            Guardian? guardian = null;

            if (students.Any(s => s.IsMinor))
            {
                guardian = contract.Parties
                    .FirstOrDefault(p => p.GuardianId != null)
                    ?.Guardian;
            }

            act.Body = await GenerateAdditionalActBody(contract, act, guardian, students);
            act.PdfPath = null;
            await _db.SaveChangesAsync();

            return response.SetSuccess(new { act.Id });
        }

        public async Task<PublicResponse> FinalizeAdditionalActAsync(int id)
        {
            var response = new PublicResponse(true);

            var act = await _db.ContractAdditionalAct
                .Include(a => a.Contract)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (act == null)
                return response.SetError(ErrorCodes.InvalidParameters, "Act not found");

            if (act.Status != AdditionalActStatus.Draft)
                return response.SetError(ErrorCodes.InvalidParameters, "Already finalized");

            act.Status = AdditionalActStatus.Finalized;

            _activityLogService.Add(
                 nameof(ContractAdditionalAct),
                 act.Id.ToString(),
                 "Finalized",
                 $"Act {act.ActNumber} finalizat"
             );

            await _db.SaveChangesAsync();

            await _notificationsService.CreateNotificationForRolesAsync(
                roleNames: new[] { "Admin", "Secretary" },
                eventType: NotificationEvents.ContractActivity,
                title: "Act adițional finalizat",
                message: $"Actul adițional {act.ActNumber} a fost finalizat.",
                type: "Info",
                link: $"/contracts/{act.ContractId}/additional-acts/{act.Id}",
                entityType: nameof(ContractAdditionalAct),
                entityId: act.Id.ToString()
            );

            return response.SetSuccess(true);
        }

        public async Task<IResult> DownloadActAsync(int id)
        {
            var act = await _db.ContractAdditionalAct
                .Include(a => a.Contract)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (act == null)
                return Results.NotFound();

            if (act.Status != AdditionalActStatus.Applied)
                return Results.BadRequest("Actul adițional nu este aplicat încă.");

            if (string.IsNullOrEmpty(act.PdfPath))
                return Results.BadRequest("PDF not generated");

            var filePath = Path.Combine("wwwroot", "contracts", act.PdfPath);

            if (!File.Exists(filePath))
                return Results.NotFound("PDF file not found");

            var bytes = await File.ReadAllBytesAsync(filePath);

            return Results.File(
                bytes,
                "application/pdf",
                $"Act_{act.ActNumber}.pdf"
            );
        }

        public async Task<PublicResponse> GetAdditionalActByIdAsync(int id)
        {
            var response = new PublicResponse(true);

            var act = await _db.ContractAdditionalAct
                .AsNoTracking()
                .Include(a => a.Items)
                .Include(a => a.Contract)
                    .ThenInclude(c => c.Parties)
                        .ThenInclude(p => p.Student)
                .Include(a => a.Contract)
                    .ThenInclude(c => c.Parties)
                        .ThenInclude(p => p.Guardian)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (act == null)
                return response.SetError(ErrorCodes.InvalidParameters, "Not found");

            var dto = new AdditionalActDetailsDto
            {
                Id = act.Id,
                ActNumber = act.ActNumber,
                Status = act.Status.ToString(),
                Description = act.Description,
                Body = act.Body,
                CreatedAtUtc = act.CreatedAtUtc,
                ContractId = act.ContractId,

                // Parties
                Parties = act.Contract.Parties
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

                // Items
                Items = act.Items
                   .Select(i => new AdditionalActItemDto
                   {
                       Type = i.Type.ToString(),
                       CourseSessionId = i.CourseSessionId,
                       NewValue = i.NewValue
                   })
                   .ToList()
                       };

            return response.SetSuccess(dto);
        }

        public async Task<PublicResponse> ListAdditionalActsAsync(int contractId)
        {
            var response = new PublicResponse(true);

            var acts = await _db.ContractAdditionalAct
                .Where(a => a.ContractId == contractId)
                .Include(a => a.Items)
                .Include(a => a.Contract)
                    .ThenInclude(c => c.Parties)
                        .ThenInclude(p => p.Student)
                .Include(a => a.Contract)
                    .ThenInclude(c => c.Parties)
                        .ThenInclude(p => p.Guardian)
                .OrderByDescending(a => a.CreatedAtUtc)
                .ToListAsync();

            var result = acts.Select(a => new AdditionalActDetailsDto
            {
                Id = a.Id,
                ActNumber = a.ActNumber,
                Status = a.Status.ToString(),
                Description = a.Description,
                Body = a.Body,
                CreatedAtUtc = a.CreatedAtUtc,
                ContractId = a.ContractId,

                // PARTIES
                Parties = a.Contract.Parties
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

                // ITEMS
                Items = a.Items
                   .Select(i => new AdditionalActItemDto
                   {
                       Type = i.Type.ToString(),
                       CourseSessionId = i.CourseSessionId,
                       NewValue = i.NewValue
                   })
                   .ToList()
                     }).ToList();

            return response.SetSuccess(result);
        }

        private string GenerateActNumber()
        {
            return $"AA-{DateTime.UtcNow:yyyy}-{DateTime.UtcNow.Ticks.ToString()[^5..]}";
        }

        private async Task<string> GenerateAdditionalActBody(StudentContract contract, ContractAdditionalAct act, Guardian? guardian, List<Data.Entities.Student> students)
        {
            var template = await _db.ContractTemplates
                .Where(x => x.IsActive && x.Name == "Default Additional Act Template")
                .Select(x => x.Body)
                .FirstAsync();

            var studentList = string.Join(", ", students.Select(s => s.FullName));

            var coursesList = string.Join("\n",
                contract.Courses.Select(c =>
                    $"- {c.CourseNameSnapshot} ({c.SessionNameSnapshot}) – {c.PriceSnapshot} RON"));

            var effectiveDate = DateTime.UtcNow.ToString("dd.MM.yyyy");

            var sessionIds = act.Items
               .Where(i => i.CourseSessionId.HasValue)
               .Select(i => i.CourseSessionId.Value)
               .ToList();

            var sessions = await _db.CourseSessions
                .Include(s => s.Course)
                .Where(s => sessionIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            var changes = string.Join("<br/>",
                 act.Items.Select(i =>
                 {
                     return i.Type switch
                     {
                         AdditionalActType.AddCourse when i.CourseSessionId.HasValue =>
                             $"• Curs adăugat: {sessions[i.CourseSessionId.Value].Course.Name} (+{i.NewValue} RON)",
             
                         AdditionalActType.RemoveCourse when i.CourseSessionId.HasValue =>
                             $"• Curs eliminat: {sessions[i.CourseSessionId.Value].Course.Name} (-{i.NewValue} RON)",

                         AdditionalActType.ExtendPeriod =>
                              DateTime.TryParse(i.NewValue, out var parsedDate)
                              ? $"• Perioadă extinsă până la {parsedDate:dd.MM.yyyy}" : $"• Perioadă extinsă până la {i.NewValue}",

                         AdditionalActType.AddDiscount =>
                             $"• Discount aplicat: -{i.NewValue} RON",
             
                         AdditionalActType.IncreasePrice =>
                             $"• Majorare preț: +{i.NewValue} RON",
             
                         _ => ""
                     };
                 })
                 .Where(x => !string.IsNullOrWhiteSpace(x))
             );

            var values = new Dictionary<string, string>
            {
                ["ActNumber"] = act.ActNumber,
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

                ["ActDescription"] = act.Description,
                ["Changes"] = changes,
                ["EffectiveDate"] = effectiveDate,

                ["Total"] = contract.TotalAmount.HasValue ? $"{contract.TotalAmount.Value:0.00} RON" : "-",

                ["MonthlyAmount"] = $"{contract.MonthlyAmount:0.00} RON",

                ["ContractEndDate"] = contract.EndDate.HasValue  ? contract.EndDate.Value.ToString("dd.MM.yyyy") : "Nelimitat",

                ["AdminSignDate"] = contract.AdminSignedAtUtc?.ToString("dd.MM.yyyy") ?? "",
                ["ClientSignDate"] = contract.ClientSignedAtUtc?.ToString("dd.MM.yyyy") ?? ""
            };

            return _templateRenderer.Render(template, values);
        }

        public async Task<PublicResponse> ApplyAdditionalActAsync(int actId, bool saveChanges = true)
        {
            var response = new PublicResponse(true);

            var act = await _db.ContractAdditionalAct
              .Include(a => a.Contract)
                  .ThenInclude(c => c.InstallmentsList)
              .Include(a => a.Contract)
                  .ThenInclude(c => c.Discounts)
              .Include(a => a.Contract)
                  .ThenInclude(c => c.PriceAdjustments)
                      .ThenInclude(p => p.CourseSession)
              .Include(a => a.Items)
              .FirstOrDefaultAsync(a => a.Id == actId);

            if (act == null)
                return response.SetError(ErrorCodes.InvalidParameters, "Act not found");

            if (act.Status == AdditionalActStatus.Applied)
                return response.SetError(ErrorCodes.InvalidParameters, "Act already applied");

            if (act.Status != AdditionalActStatus.SignedByClient)
                return response.SetError(ErrorCodes.InvalidParameters, "Act must be signed by client before applying");

            var contract = act.Contract;

            if (contract.Status != ContractStatus.Active)
                return response.SetError(ErrorCodes.InvalidParameters, "Contract not active");

            foreach (var item in act.Items)
            {
                switch (item.Type)
                {
                    case AdditionalActType.AddCourse:
                        {
                            if (!item.CourseSessionId.HasValue)
                                return response.SetError(ErrorCodes.InvalidParameters, "Course required");

                            var enrollment = await _db.CourseEnrollments
                                .FirstOrDefaultAsync(e =>
                                    e.CourseSessionId == item.CourseSessionId.Value &&
                                    e.StudentId != null &&
                                    e.ContractId == null &&
                                    e.IsActive);

                            if (enrollment == null)
                                return response.SetError(ErrorCodes.InvalidParameters, "Enrollment not found or already in contract");

                            enrollment.ContractId = contract.Id;

                            break;
                        }

                    case AdditionalActType.RemoveCourse:
                        {
                            if (!item.CourseSessionId.HasValue)
                                return response.SetError(ErrorCodes.InvalidParameters, "Course required");

                            var existing = await _db.CourseEnrollments
                                .FirstOrDefaultAsync(e =>
                                    e.CourseSessionId == item.CourseSessionId.Value &&
                                    e.ContractId == contract.Id &&
                                    !e.IsActive);

                            if (existing == null)
                                return response.SetError(ErrorCodes.InvalidParameters, "Removed course not found in contract");

                            existing.ContractId = null;

                            break;
                        }

                    case AdditionalActType.ExtendPeriod:
                        {
                            if (!DateTime.TryParse(item.NewValue, out var newDate))
                                return response.SetError(ErrorCodes.InvalidParameters, "Invalid end date");

                            contract.EndDate = newDate;
                            break;
                        }

                    case AdditionalActType.AddDiscount:
                        {
                            if (!item.CourseSessionId.HasValue)
                                return response.SetError(ErrorCodes.InvalidParameters, "Course session required for discount");

                            if (!decimal.TryParse(item.NewValue, out var discount) || discount <= 0)
                                return response.SetError(ErrorCodes.InvalidParameters, "Invalid discount");

                            contract.PriceAdjustments.Add(new ContractPriceAdjustment
                            {
                                ContractId = contract.Id,
                                CourseSessionId = item.CourseSessionId.Value,
                                Type = PriceAdjustmentType.Discount,
                                Amount = discount,
                                Reason = $"Act adițional {act.ActNumber}",
                                CreatedAtUtc = DateTime.UtcNow
                            });

                            break;
                        }

                    case AdditionalActType.IncreasePrice:
                        {
                            if (!item.CourseSessionId.HasValue)
                                return response.SetError(ErrorCodes.InvalidParameters, "Course session required for increase");

                            if (!decimal.TryParse(item.NewValue, out var increase) || increase <= 0)
                                return response.SetError(ErrorCodes.InvalidParameters, "Invalid increase");

                            contract.PriceAdjustments.Add(new ContractPriceAdjustment
                            {
                                ContractId = contract.Id,
                                CourseSessionId = item.CourseSessionId.Value,
                                Type = PriceAdjustmentType.Increase,
                                Amount = increase,
                                Reason = $"Act adițional {act.ActNumber}",
                                CreatedAtUtc = DateTime.UtcNow
                            });

                            break;
                        }

                    default:
                        return response.SetError(ErrorCodes.InvalidParameters, "Unsupported change type");
                }
            }

            var activeSessionIds = await _db.CourseEnrollments
                .Where(e => e.ContractId == contract.Id && e.IsActive)
                .Select(e => e.CourseSessionId)
                .Distinct()
                .ToListAsync();

            var sessions = await _db.CourseSessions
                .Where(s => activeSessionIds.Contains(s.Id))
                .ToListAsync();

            var pricing = _pricingService.CalculatePricing(sessions, contract);

            var appliedDate = DateTime.UtcNow.Date;

            contract.MonthlyAmount = pricing.MonthlyAmount;
            contract.TotalAmount = pricing.TotalAmount;

            var unpaidInstallments = contract.InstallmentsList
                .Where(i =>
                    !i.IsPaid &&
                    i.DueDate.Date >= appliedDate)
                .ToList();

            var installmentStartDate = unpaidInstallments.Any()
                ? unpaidInstallments.Min(i => i.DueDate.Date)
                : appliedDate;

            _db.ContractInstallments.RemoveRange(unpaidInstallments);

            var newInstallments = _installmentService.BuildInstallmentsForRemainingPeriod(
               installmentStartDate,
               contract.EndDate ?? installmentStartDate,
               contract.IsUnlimited,
               pricing
            );

            foreach (var installment in newInstallments)
            {
                installment.ContractId = contract.Id;
            }

            _db.ContractInstallments.AddRange(newInstallments);

            act.Status = AdditionalActStatus.Applied;
            act.AppliedAtUtc = DateTime.UtcNow;

            _activityLogService.Add(
                nameof(ContractAdditionalAct),
                act.Id.ToString(),
                "Applied",
                $"Actul adițional {act.ActNumber} a fost aplicat"
            );
            if (saveChanges)
            {
                await _db.SaveChangesAsync();

                act.PdfPath = _pdfService.GenerateAdditionalActPdf(act);

                await _db.SaveChangesAsync();
            }
            else
            {
                act.PdfPath = _pdfService.GenerateAdditionalActPdf(act);
            }

            return response.SetSuccess();
        }

        private async Task<(List<ContractAdditionalActItem> Items, string Description)> BuildAdditionalActItemsAsync( StudentContract contract, CreateAdditionalActDto dto, int? actId = null)
        {
            var items = new List<ContractAdditionalActItem>();
            var descriptions = new List<string>();

            if (dto.Types == null || !dto.Types.Any())
                throw new InvalidOperationException("At least one type required");

            var studentId = contract.Parties
                .Where(p => p.StudentId.HasValue)
                .Select(p => p.StudentId!.Value)
                .FirstOrDefault();

            if (studentId == 0)
                throw new InvalidOperationException("Student not found");

            var workingMonthly = contract.MonthlyAmount;
            var workingTotal = contract.TotalAmount;

            var selectedSessionIds = dto.AddCourseSessionIds
              .Concat(dto.RemoveCourseSessionIds)
              .Distinct()
              .ToList();

            var openActStatuses = new[]
            {
                  AdditionalActStatus.Draft,
                  AdditionalActStatus.Finalized,
                  AdditionalActStatus.SentToClient,
                  AdditionalActStatus.SignedByClient
              };

            if (selectedSessionIds.Any())
            {
                var alreadyUsed = await _db.ContractAdditionalAct
                    .Where(a =>
                        a.ContractId == contract.Id &&
                        a.Id != actId &&
                        openActStatuses.Contains(a.Status))
                    .SelectMany(a => a.Items)
                    .Where(i =>
                        i.CourseSessionId.HasValue &&
                        selectedSessionIds.Contains(i.CourseSessionId.Value))
                    .AnyAsync();

                if (alreadyUsed)
                    throw new InvalidOperationException("Unul dintre cursuri este deja inclus într-un alt act adițional neaplicat.");
            }

            foreach (var type in dto.Types.Distinct())
            {
                switch (type)
                {
                    case AdditionalActType.AddCourse:
                        {
                            if (!dto.AddCourseSessionIds.Any())
                                throw new InvalidOperationException("Course required");

                            foreach (var sessionId in dto.AddCourseSessionIds.Distinct())
                            {
                                var enrollment = await _db.CourseEnrollments
                                    .Include(e => e.Session)
                                        .ThenInclude(s => s.Course)
                                    .FirstOrDefaultAsync(e =>
                                        e.CourseSessionId == sessionId &&
                                        e.StudentId == studentId &&
                                        e.IsActive);

                                if (enrollment == null)
                                    throw new InvalidOperationException("Enrollment not found");

                                if (enrollment.ContractId != null)
                                    throw new InvalidOperationException("Already in contract");

                                var price = enrollment.Session.Fee;

                                workingMonthly += price;

                                if (workingTotal.HasValue)
                                    workingTotal += price;

                                items.Add(new ContractAdditionalActItem
                                {
                                    ActId = actId ?? 0,
                                    Type = type,
                                    CourseSessionId = sessionId,
                                    NewValue = price.ToString("0.00")
                                });

                                descriptions.Add($"Adăugat curs: {enrollment.Session.Course.Name} (+{price:0.00} RON)");
                            }

                            break;
                        }

                    case AdditionalActType.RemoveCourse:
                        {
                            if (!dto.RemoveCourseSessionIds.Any())
                                throw new InvalidOperationException("Course required");

                            foreach (var sessionId in dto.RemoveCourseSessionIds.Distinct())
                            {
                                var existingEnrollment = await _db.CourseEnrollments
                                    .Include(e => e.Session)
                                        .ThenInclude(s => s.Course)
                                    .FirstOrDefaultAsync(e =>
                                        e.CourseSessionId == sessionId &&
                                        e.ContractId == contract.Id &&
                                        !e.IsActive);

                                if (existingEnrollment == null)
                                    throw new InvalidOperationException("Course not removed from contract");

                                var price = existingEnrollment.Session.Fee;

                                workingMonthly = Math.Max(0, workingMonthly - price);

                                if (workingTotal.HasValue)
                                    workingTotal = Math.Max(0, workingTotal.Value - price);

                                items.Add(new ContractAdditionalActItem
                                {
                                    ActId = actId ?? 0,
                                    Type = type,
                                    CourseSessionId = sessionId,
                                    NewValue = price.ToString("0.00")
                                });

                                descriptions.Add($"Eliminat curs: {existingEnrollment.Session.Course.Name} (-{price:0.00} RON)");
                            }

                            break;
                        }

                    case AdditionalActType.ExtendPeriod:
                        {
                            if (!dto.NewEndDate.HasValue)
                                throw new InvalidOperationException("New date required");

                            if (contract.EndDate.HasValue &&
                                dto.NewEndDate.Value <= contract.EndDate.Value)
                            {
                                throw new InvalidOperationException("New end date must be after current end date");
                            }

                            items.Add(new ContractAdditionalActItem
                            {
                                ActId = actId ?? 0,
                                Type = type,
                                NewValue = dto.NewEndDate.Value.ToString("yyyy-MM-dd")
                            });

                            if (workingTotal.HasValue)
                            {
                                var months = CalculateMonths(contract.StartDate, dto.NewEndDate.Value);
                                workingTotal = workingMonthly * months;
                            }

                            descriptions.Add($"Extins până la {dto.NewEndDate.Value:dd.MM.yyyy}");

                            break;
                        }

                    case AdditionalActType.AddDiscount:
                        {
                            if (dto.PriceAdjustments == null || !dto.PriceAdjustments.Any())
                                throw new InvalidOperationException("Discount required");

                            foreach (var adj in dto.PriceAdjustments)
                            {
                                if (adj.CourseSessionId <= 0 || adj.Amount <= 0)
                                    throw new InvalidOperationException("Invalid discount");

                                items.Add(new ContractAdditionalActItem
                                {
                                    ActId = actId ?? 0,
                                    Type = type,
                                    CourseSessionId = adj.CourseSessionId,
                                    NewValue = adj.Amount.ToString("0.00")
                                });

                                descriptions.Add($"Discount aplicat pentru sesiunea #{adj.CourseSessionId}: -{adj.Amount:0.00} RON");
                            }

                            break;
                        }

                    case AdditionalActType.IncreasePrice:
                        {
                            if (dto.PriceAdjustments == null || !dto.PriceAdjustments.Any())
                                throw new InvalidOperationException("Increase required");

                            foreach (var adj in dto.PriceAdjustments)
                            {
                                if (adj.CourseSessionId <= 0 || adj.Amount <= 0)
                                    throw new InvalidOperationException("Invalid increase");

                                items.Add(new ContractAdditionalActItem
                                {
                                    ActId = actId ?? 0,
                                    Type = type,
                                    CourseSessionId = adj.CourseSessionId,
                                    NewValue = adj.Amount.ToString("0.00")
                                });

                                descriptions.Add($"Majorare aplicată pentru sesiunea #{adj.CourseSessionId}: +{adj.Amount:0.00} RON");
                            }

                            break;
                        }

                    default:
                        throw new InvalidOperationException("Unsupported change type");
                }
            }

            return (items, string.Join(" | ", descriptions));
        }

        private int CalculateMonths(DateTime start, DateTime end)
        {
            return (end.Year - start.Year) * 12 +
                   (end.Month - start.Month) + 1;
        }
    }
}
