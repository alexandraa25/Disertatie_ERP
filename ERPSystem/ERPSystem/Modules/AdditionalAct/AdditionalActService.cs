using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.AdditionalAct.Models;
using ERPSystem.Modules.Contracts;
using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Shared.BusinessLogic;
using ERPSystem.Shared.DTOs.PDF;
using ERPSystem.Utils.Constants.Email;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Enums;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPSystem.Modules.AdditionalAct
{
    public class AdditionalActService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ContractsService> _logger;
        private readonly PdfService _pdfService;
        private readonly EmailBusinessLogic _emailBusinessLogic;

        public AdditionalActService(ApplicationDbContext db, ILogger<ContractsService> logger, EmailBusinessLogic emailBusinessLogic, PdfService pdfService)
        {
            _db = db;
            _logger = logger;
            _emailBusinessLogic = emailBusinessLogic;
            _pdfService = pdfService;
        }

        public async Task<PublicResponse> CreateAdditionalActAsync(int contractId, CreateAdditionalActDto dto)
        {
            var response = new PublicResponse(true);

            var contract = await _db.StudentContracts
                .Include(c => c.Parties)
                .Include(c => c.InstallmentsList)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
                return response.SetError(ErrorCodes.InvalidParameters, "Contract not found");

            if (contract.Status != ContractStatus.Active)
                return response.SetError(ErrorCodes.InvalidParameters, "Only active contracts can have additional acts");

            if (dto.Types == null || !dto.Types.Any())
                return response.SetError(ErrorCodes.InvalidParameters, "At least one type required");

            var descriptions = new List<string>();

            var studentId = contract.Parties
                .Where(p => p.StudentId != null)
                .Select(p => p.StudentId.Value)
                .FirstOrDefault();

            var act = new ContractAdditionalAct
            {
                ContractId = contract.Id,
                ActNumber = GenerateActNumber(),
                Status = AdditionalActStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow,
                Items = new List<ContractAdditionalActItem>()
            };

            // 🔥 NOUA LOGICĂ
            var workingMonthly = contract.MonthlyAmount;
            var workingTotal = contract.TotalAmount;

            foreach (var type in dto.Types)
            {
                var item = new ContractAdditionalActItem
                {
                    Type = type
                };

                switch (type)
                {
                    // =========================
                    // ADD COURSE
                    // =========================
                    case AdditionalActType.AddCourse:

                        if (!dto.CourseSessionIds?.Any() ?? true)
                            return response.SetError(ErrorCodes.InvalidParameters, "Course required");

                        var sessionId = dto.CourseSessionIds.First();

                        var enrollment = await _db.CourseEnrollments
                            .Include(e => e.Session)
                                .ThenInclude(s => s.Course)
                            .FirstOrDefaultAsync(e =>
                                e.CourseSessionId == sessionId &&
                                e.StudentId == studentId &&
                                e.IsActive);

                        if (enrollment == null)
                            return response.SetError(ErrorCodes.InvalidParameters, "Enrollment not found");

                        if (enrollment.ContractId != null)
                            return response.SetError(ErrorCodes.InvalidParameters, "Already in contract");

                        var price = enrollment.Session.Fee;

                        // 🔥 UPDATE
                        workingMonthly += price;

                        if (workingTotal.HasValue)
                            workingTotal += price;

                        item.CourseSessionId = sessionId;
                        item.NewValue = workingTotal?.ToString("0.00")
                            ?? workingMonthly.ToString("0.00");

                        descriptions.Add($"Adăugat curs: {enrollment.Session.Course.Name} (+{price} RON)");

                        break;

                    // =========================
                    // REMOVE COURSE
                    // =========================
                    case AdditionalActType.RemoveCourse:

                        if (!dto.CourseSessionIds?.Any() ?? true)
                            return response.SetError(ErrorCodes.InvalidParameters, "Course required");

                        var removeSessionId = dto.CourseSessionIds.First();

                        var existingEnrollment = await _db.CourseEnrollments
                            .Include(e => e.Session)
                                .ThenInclude(s => s.Course)
                            .FirstOrDefaultAsync(e =>
                                e.CourseSessionId == removeSessionId &&
                                e.ContractId == contract.Id);

                        if (existingEnrollment == null)
                            return response.SetError(ErrorCodes.InvalidParameters, "Course not in contract");

                        var removePrice = existingEnrollment.Session.Fee;

                        workingMonthly = Math.Max(0, workingMonthly - removePrice);

                        if (workingTotal.HasValue)
                            workingTotal = Math.Max(0, workingTotal.Value - removePrice);

                        item.CourseSessionId = removeSessionId;
                        item.NewValue = workingTotal?.ToString("0.00")
                            ?? workingMonthly.ToString("0.00");

                        descriptions.Add($"Eliminat curs: {existingEnrollment.Session.Course.Name} (-{removePrice} RON)");

                        break;

                    // =========================
                    // EXTEND PERIOD
                    // =========================
                    case AdditionalActType.ExtendPeriod:

                        if (!dto.NewEndDate.HasValue)
                            return response.SetError(ErrorCodes.InvalidParameters, "New date required");

                        item.NewValue = dto.NewEndDate.Value.ToString("yyyy-MM-dd");

                        // 🔥 recalcul total dacă NU e abonament
                        if (workingTotal.HasValue)
                        {
                            var months = CalculateMonths(contract.StartDate, dto.NewEndDate.Value);
                            workingTotal = workingMonthly * months;
                        }

                        descriptions.Add($"Extins până la {dto.NewEndDate.Value:dd.MM.yyyy}");

                        break;

                    // =========================
                    // CHANGE PRICE
                    // =========================
                    case AdditionalActType.ChangePrice:

                        if (!dto.NewPrice.HasValue || dto.NewPrice <= 0)
                            return response.SetError(ErrorCodes.InvalidParameters, "Invalid price");

                        if (workingTotal.HasValue)
                        {
                            workingTotal = dto.NewPrice.Value;
                        }
                        else
                        {
                            workingMonthly = dto.NewPrice.Value;
                        }

                        item.NewValue = dto.NewPrice.Value.ToString("0.00");

                        descriptions.Add($"Preț modificat la {dto.NewPrice} RON");

                        break;
                }

                act.Items.Add(item);
            }

            act.Description = string.Join(" | ", descriptions);

            // =========================
            // STUDENTS + GUARDIAN
            // =========================
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

            // =========================
            // BODY GENERATION
            // =========================
            act.Body = await GenerateAdditionalActBody(contract, act, guardian, students);

            _db.ContractAdditionalAct.Add(act);

            await _db.SaveChangesAsync();

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
                .FirstOrDefaultAsync(a => a.Id == id);

            if (act == null)
                return response.SetError(ErrorCodes.InvalidParameters, "Act not found");

            if (act.Status != AdditionalActStatus.Draft)
                return response.SetError(ErrorCodes.InvalidParameters, "Only draft editable");

            var contract = act.Contract;

            act.Items.Clear();

            var descriptions = new List<string>();

            var studentId = contract.Parties
                .Where(p => p.StudentId != null)
                .Select(p => p.StudentId.Value)
                .FirstOrDefault();

            foreach (var type in dto.Types)
            {
                var item = new ContractAdditionalActItem
                {
                    ActId = act.Id,
                    Type = type
                };

                switch (type)
                {
                    case AdditionalActType.AddCourse:

                        var sessionId = dto.CourseSessionIds?.FirstOrDefault();

                        var enrollment = await _db.CourseEnrollments
                            .Include(e => e.Session)
                                .ThenInclude(s => s.Course)
                            .FirstOrDefaultAsync(e =>
                                e.CourseSessionId == sessionId &&
                                e.StudentId == studentId &&
                                e.IsActive);

                        if (enrollment == null)
                            return response.SetError(ErrorCodes.InvalidParameters, "Enrollment not found");

                        var addPrice = enrollment.Session.Fee;
                        var newTotalAdd = contract.TotalAmount + addPrice;

                        item.CourseSessionId = sessionId;
                        item.NewValue = newTotalAdd.ToString();

                        descriptions.Add($"Adăugat curs: {enrollment.Session.Course.Name} (+{addPrice} RON → total {newTotalAdd})");
                        break;

                    case AdditionalActType.RemoveCourse:

                        var removeSessionId = dto.CourseSessionIds?.FirstOrDefault();

                        var existingEnrollment = await _db.CourseEnrollments
                            .Include(e => e.Session)
                                .ThenInclude(s => s.Course)
                            .FirstOrDefaultAsync(e =>
                                e.CourseSessionId == removeSessionId &&
                                e.ContractId == contract.Id);

                        if (existingEnrollment == null)
                            return response.SetError(ErrorCodes.InvalidParameters, "Course not in contract");

                        var removePrice = existingEnrollment.Session.Fee;
                        var newTotalRemove = contract.TotalAmount - removePrice;

                        item.CourseSessionId = removeSessionId;
                        item.NewValue = newTotalRemove.ToString();

                        descriptions.Add($"Eliminat curs: {existingEnrollment.Session.Course.Name} (-{removePrice} RON → total {newTotalRemove})");
                        break;

                    case AdditionalActType.ExtendPeriod:

                        if (!dto.NewEndDate.HasValue)
                            return response.SetError(ErrorCodes.InvalidParameters, "New date required");

                        item.NewValue = dto.NewEndDate.Value.ToString("yyyy-MM-dd");

                        descriptions.Add($"Extins până la {dto.NewEndDate.Value:dd.MM.yyyy}");
                        break;

                    case AdditionalActType.ChangePrice:

                        if (!dto.NewPrice.HasValue || dto.NewPrice <= 0)
                            return response.SetError(ErrorCodes.InvalidParameters, "Invalid price");

                        item.NewValue = dto.NewPrice.Value.ToString();

                        descriptions.Add($"Preț modificat la {dto.NewPrice} RON");
                        break;
                }

                act.Items.Add(item);
            }

            act.Description = string.Join(" | ", descriptions);

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

            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = nameof(ContractAdditionalAct),
                EntityId = act.Id.ToString(),
                Action = "Finalized",
                Description = $"Act {act.ActNumber} finalizat",
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return response.SetSuccess(true);
        }

        public async Task<IResult> DownloadActAsync(int id)
        {
            var act = await _db.ContractAdditionalAct
                .Include(a => a.Contract)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (act == null)
                return Results.NotFound();

            if (string.IsNullOrEmpty(act.PdfPath))
                return Results.BadRequest("PDF not generated");

            var filePath = Path.Combine("wwwroot", "contracts", act.PdfPath);

            if (!File.Exists(filePath))
            {
                // 🔥 regenerează PDF dacă lipsește
                var fileName = _pdfService.GeneratePdf(new PdfDocumentModel
                {
                    Title = "ACT ADIȚIONAL",
                    Number = act.ActNumber,
                    Body = act.Body,
                    CompanyName = act.Contract.CompanyNameSnapshot,
                    BeneficiaryName = act.Contract.BeneficiaryNameSnapshot,
                    AdminSignature = act.AdminSignature,
                    ClientSignature = act.ClientSignature,
                    AdminSignedAt = act.AdminSignedAtUtc,
                    ClientSignedAt = act.ClientSignedAtUtc
                }, "ACT");

                act.PdfPath = fileName;
                await _db.SaveChangesAsync();

                filePath = Path.Combine("wwwroot", "contracts", fileName);
            }

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

            var dto = new AdditionalActDetailsDto(
                act.Id,
                act.ActNumber,
                act.Status.ToString(),
                act.Description,
                act.Body,
                act.CreatedAtUtc,
                act.ContractId,

                // 🔥 Parties (opțional, dar util)
                act.Contract.Parties.Select(p => new ContractPartyDto(
                    p.StudentId,
                    p.Student != null ? p.Student.FirstName + " " + p.Student.LastName : null,
                    p.GuardianId,
                    p.Guardian != null ? p.Guardian.FirstName + " " + p.Guardian.LastName : null,
                    p.Role.ToString()
                )).ToList(),

                // 🔥 Items
                act.Items.Select(i => new AdditionalActItemDto(
                    i.Type.ToString(),
                    i.CourseSessionId,
                    i.NewValue
                )).ToList()
            );

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

            var result = acts.Select(a => new AdditionalActDetailsDto(
                a.Id,
                a.ActNumber,
                a.Status.ToString(),
                a.Description,
                a.Body,
                a.CreatedAtUtc,
                a.ContractId,

                // 🔥 PARTIES
                a.Contract.Parties.Select(p => new ContractPartyDto(
                    p.StudentId,
                    p.Student != null ? p.Student.FirstName + " " + p.Student.LastName : null,
                    p.GuardianId,
                    p.Guardian != null ? p.Guardian.FirstName + " " + p.Guardian.LastName : null,
                    p.Role.ToString()
                )).ToList(),

                // 🔥 ITEMS
                a.Items.Select(i => new AdditionalActItemDto(
                    i.Type.ToString(),
                    i.CourseSessionId,
                    i.NewValue
                )).ToList()
            )).ToList();

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
            // 🔥 schimbări (logică în funcție de tip)

            var changes = string.Join("\n",
             act.Items.Select(i =>
             {
                 return i.Type switch
                 {
                     AdditionalActType.AddCourse =>
                         $"• Curs adăugat: {sessions[i.CourseSessionId.Value].Course.Name}",

                     AdditionalActType.RemoveCourse =>
                         $"• Curs eliminat: {sessions[i.CourseSessionId.Value].Course.Name}",

                     AdditionalActType.ExtendPeriod =>
                         $"• Perioadă extinsă până la {i.NewValue}",

                     AdditionalActType.ChangePrice =>
                         $"• Preț modificat la {i.NewValue} RON",

                     _ => ""
                 };
             }));

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

                ["AdminSignDate"] = contract.AdminSignedAtUtc?.ToString("dd.MM.yyyy") ?? "",
                ["ClientSignDate"] = contract.ClientSignedAtUtc?.ToString("dd.MM.yyyy") ?? ""
            };

            return ApplyTemplate(template, values);
        }

        private string ApplyTemplate(string template, Dictionary<string, string> values)
        {
            foreach (var item in values)
            {
                template = template.Replace($"{{{{{item.Key}}}}}", item.Value ?? "");
            }

            return template;
        }


        public async Task<PublicResponse> ApplyAdditionalActAsync(int actId)
        {
            var response = new PublicResponse(true);

            var act = await _db.ContractAdditionalAct
                .Include(a => a.Contract)
                    .ThenInclude(c => c.InstallmentsList)
                .Include(a => a.Items)
                .FirstOrDefaultAsync(a => a.Id == actId);

            if (act == null)
                return response.SetError(ErrorCodes.InvalidParameters, "Act not found");

           
            var contract = act.Contract;

            if (contract.Status != ContractStatus.Active)
                return response.SetError(ErrorCodes.InvalidParameters, "Contract not active");

            // 🔥 calcul working values (NU modificăm contract direct!)
            var workingMonthly = contract.InstallmentsList
                .OrderBy(i => i.DueDate)
                .Select(i => i.Amount)
                .FirstOrDefault();

            var workingTotal = contract.TotalAmount;

            foreach (var item in act.Items)
            {
                switch (item.Type)
                {
                    // =========================
                    // ADD COURSE
                    // =========================
                    case AdditionalActType.AddCourse:

                        var enrollment = await _db.CourseEnrollments
                            .FirstOrDefaultAsync(e =>
                                e.CourseSessionId == item.CourseSessionId &&
                                e.StudentId != null &&
                                e.ContractId == null);

                        if (enrollment != null)
                        {
                            enrollment.ContractId = contract.Id;
                        }

                        var addPrice = await _db.CourseSessions
                            .Where(s => s.Id == item.CourseSessionId)
                            .Select(s => s.Fee)
                            .FirstOrDefaultAsync();

                        workingMonthly += addPrice;

                        if (workingTotal.HasValue)
                            workingTotal += addPrice;

                        break;

                    // =========================
                    // REMOVE COURSE
                    // =========================
                    case AdditionalActType.RemoveCourse:

                        var existing = await _db.CourseEnrollments
                            .FirstOrDefaultAsync(e =>
                                e.CourseSessionId == item.CourseSessionId &&
                                e.ContractId == contract.Id);

                        if (existing != null)
                        {
                            existing.ContractId = null;
                        }

                        var removePrice = await _db.CourseSessions
                            .Where(s => s.Id == item.CourseSessionId)
                            .Select(s => s.Fee)
                            .FirstOrDefaultAsync();

                        workingMonthly = Math.Max(0, workingMonthly - removePrice);

                        if (workingTotal.HasValue)
                            workingTotal = Math.Max(0, workingTotal.Value - removePrice);

                        break;

                    // =========================
                    // EXTEND PERIOD
                    // =========================
                    case AdditionalActType.ExtendPeriod:

                        if (DateTime.TryParse(item.NewValue, out var newDate))
                        {
                            contract.EndDate = newDate;

                            if (workingTotal.HasValue)
                            {
                                var months = CalculateMonths(contract.StartDate, newDate);
                                workingTotal = workingMonthly * months;
                            }
                        }

                        break;

                    // =========================
                    // CHANGE PRICE
                    // =========================
                    case AdditionalActType.ChangePrice:

                        if (decimal.TryParse(item.NewValue, out var newPrice))
                        {
                            if (workingTotal.HasValue)
                            {
                                workingTotal = newPrice;
                            }
                            else
                            {
                                workingMonthly = newPrice;
                            }
                        }

                        break;
                }
            }

            // =========================
            // 🔥 UPDATE INSTALLMENTS
            // =========================
            if (contract.InstallmentsList.Any())
            {
                foreach (var inst in contract.InstallmentsList)
                {
                    inst.Amount = workingMonthly;
                }
            }

            // =========================
            // 🔥 OPTIONAL: update total (DOAR pentru calcul intern)
            // =========================
            if (workingTotal.HasValue)
            {
                contract.TotalAmount = workingTotal;
            }

            // ⚠️ NU modificăm ContractBody!
            // ⚠️ NU regenerăm PDF!

            act.Status = AdditionalActStatus.Applied;

            await _db.SaveChangesAsync();

            return response.SetSuccess();
        }

        private int CalculateMonths(DateTime start, DateTime end)
        {
            return (end.Year - start.Year) * 12 +
                   (end.Month - start.Month) + 1;
        }
    }
}
