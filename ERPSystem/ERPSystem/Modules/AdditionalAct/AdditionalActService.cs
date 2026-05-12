using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.AdditionalAct.Models;
using ERPSystem.Modules.Contracts;
using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Shared.BusinessLogic;
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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdditionalActService(
            ApplicationDbContext db, 
            ILogger<ContractsService> logger, 
            PdfService pdfService, 
            NotificationsService notificationsService, 
            TemplateRendererService templateRenderer,
            ActivityLogService activityLogService,
            ContractPricingService pricingService,
            ContractInstallmentService installmentService,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _logger = logger;
            _pdfService = pdfService;
            _notificationsService = notificationsService;
            _templateRenderer = templateRenderer;
            _activityLogService = activityLogService;
            _pricingService = pricingService;
            _installmentService = installmentService;
            _httpContextAccessor = httpContextAccessor;
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
                return response.SetError(ErrorCodes.InvalidParameters, "Contractul nu a fost găsit.");

            if (contract.Status != ContractStatus.Active)
                return response.SetError(ErrorCodes.InvalidParameters, "Doar contractele active pot avea acte adiționale.");

            if (dto.Types == null || !dto.Types.Any())
                return response.SetError(ErrorCodes.InvalidParameters, "Trebuie selectat cel puțin un tip de modificare.");

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
                $"Actul adițional {act.ActNumber} a fost creat pentru contractul {contract.ContractNumber}. {act.Description}",
                 GetCurrentUser()
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
                return response.SetError(ErrorCodes.InvalidParameters, "Actul adițional nu a fost găsit.");

            if (act.Status != AdditionalActStatus.Draft)
                return response.SetError(ErrorCodes.InvalidParameters, "Doar actele adiționale în draft pot fi modificate.");

            if (string.IsNullOrWhiteSpace(dto.Body))
                return response.SetError(ErrorCodes.InvalidParameters, "Conținutul actului adițional este obligatoriu");

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
                return response.SetError(ErrorCodes.InvalidParameters, "Actul adițional nu a fost găsit.");

            if (act.Status != AdditionalActStatus.Draft)
                return response.SetError(ErrorCodes.InvalidParameters, "Doar actele adiționale în draft pot fi modificate.");

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
                return response.SetError(ErrorCodes.InvalidParameters, "Actul adițional nu a fost găsit.");

            if (act.Status != AdditionalActStatus.Draft)
                return response.SetError(ErrorCodes.InvalidParameters, "Actul adițional este deja finalizat.");

            act.Status = AdditionalActStatus.Finalized;

            _activityLogService.Add(
                 nameof(ContractAdditionalAct),
                 act.Id.ToString(),
                 "Finalized",
                 $"Act {act.ActNumber} finalizat",
                  GetCurrentUser()
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
                return Results.BadRequest("PDF-ul nu a fost generat.");

            var filePath = Path.Combine("wwwroot", "contracts", act.PdfPath);

            if (!File.Exists(filePath))
                return Results.NotFound("Fișierul PDF nu a fost găsit.");

            var bytes = await File.ReadAllBytesAsync(filePath);

            return Results.File(
                bytes,
                "application/pdf",
                $"Act_{act.ActNumber}.pdf"
            );
        }

        public async Task<PublicResponse> DeleteAdditionalActAsync(int id)
        {
            var response = new PublicResponse(true);

            var act = await _db.ContractAdditionalAct
                .Include(a => a.Items)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (act == null)
                return response.SetError(
                    ErrorCodes.InvalidParameters,
                    "Actul adițional nu a fost găsit."
                );

            if (act.Status != AdditionalActStatus.Draft)
                return response.SetError(
                    ErrorCodes.InvalidParameters,
                    "Doar actele adiționale în draft pot fi șterse."
                );

            _db.ContractAdditionalActItem.RemoveRange(act.Items);

            _db.ContractAdditionalAct.Remove(act);

            _activityLogService.Add(
                nameof(ContractAdditionalAct),
                act.Id.ToString(),
                "Delete",
                $"Actul adițional {act.ActNumber} a fost șters",
                 GetCurrentUser()
            );

            await _db.SaveChangesAsync();

            return response.SetSuccess();
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
                return response.SetError(ErrorCodes.InvalidParameters, "Actul adițional nu a fost găsit.");

            var dto = new AdditionalActDetailsDto
            {
                Id = act.Id,
                ActNumber = act.ActNumber,

                ContractId = act.ContractId,
                ContractNumber = act.Contract.ContractNumber,

                StudentId = act.Contract.Parties
                  .Where(p => p.StudentId.HasValue)
                  .Select(p => p.StudentId)
                  .FirstOrDefault(),

                Status = act.Status.ToString(),
                Description = act.Description,
                Body = act.Body,
                CreatedAtUtc = act.CreatedAtUtc,

                ClientSignature = act.ClientSignature,
                ClientSignedAtUtc = act.ClientSignedAtUtc,
                AdminSignature = act.AdminSignature,
                AdminSignedAtUtc = act.AdminSignedAtUtc,

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

            string SessionLabel(int sessionId)
            {
                var s = sessions[sessionId];

                var typeLabel = s.FeeType == CourseFeeType.Monthly
                    ? "abonament lunar"
                    : "pachet fix";

                return $"{s.Course.Name} - {s.Title}, {s.DayOfWeek}, {s.StartTime}-{s.EndTime} ({typeLabel})";
            }

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

                         AdditionalActType.AddDiscount when i.CourseSessionId.HasValue =>
                             $"• Discount aplicat pentru {SessionLabel(i.CourseSessionId.Value)}: -{i.NewValue} RON",

                         AdditionalActType.IncreasePrice when i.CourseSessionId.HasValue =>
                             $"• Majorare preț pentru {SessionLabel(i.CourseSessionId.Value)}: +{i.NewValue} RON",

                         _ => ""
                     };
                 })
                 .Where(x => !string.IsNullOrWhiteSpace(x))
             );

            var previewCourses = contract.Courses
                 .Select(c => new ContractCourse
                 {
                     ContractId = c.ContractId,
                     CourseSessionId = c.CourseSessionId,
                     CourseNameSnapshot = c.CourseNameSnapshot,
                     SessionNameSnapshot = c.SessionNameSnapshot,
                     PriceSnapshot = c.PriceSnapshot,
                     FeeType = c.FeeType
                 })
                 .ToList();

            foreach (var item in act.Items)
            {
                if (!item.CourseSessionId.HasValue)
                    continue;

                var sessionId = item.CourseSessionId.Value;

                switch (item.Type)
                {
                    case AdditionalActType.AddCourse:
                        {
                            var session = sessions[sessionId];

                            previewCourses.Add(new ContractCourse
                            {
                                ContractId = contract.Id,
                                CourseSessionId = session.Id,
                                CourseNameSnapshot = session.Course.Name,
                                SessionNameSnapshot = session.Title,
                                PriceSnapshot = session.Fee,
                                FeeType = session.FeeType
                            });

                            break;
                        }

                    case AdditionalActType.RemoveCourse:
                        {
                            previewCourses.RemoveAll(c => c.CourseSessionId == sessionId);
                            break;
                        }

                    case AdditionalActType.AddDiscount:
                        {
                            if (!decimal.TryParse(item.NewValue, out var discountValue))
                                break;

                            var course = previewCourses.FirstOrDefault(c => c.CourseSessionId == sessionId);

                            if (course != null)
                                course.PriceSnapshot = Math.Max(0, course.PriceSnapshot - discountValue);

                            break;
                        }

                    case AdditionalActType.IncreasePrice:
                        {
                            if (!decimal.TryParse(item.NewValue, out var increase))
                                break;

                            var course = previewCourses.FirstOrDefault(c => c.CourseSessionId == sessionId);

                            if (course != null)
                                course.PriceSnapshot += increase;

                            break;
                        }
                }
            }

            var previewContract = new StudentContract
            {
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                IsUnlimited = contract.IsUnlimited,
                Courses = previewCourses
            };

            var pricing = _pricingService.CalculatePricingFromContractCourses(previewContract);

            var contractType = contract.IsUnlimited
                ? "Abonament nelimitat"
                : "Contract pe perioadă determinată";

            var subtotalValue = pricing.PackageAmount + pricing.MonthlyAmount;
            var subtotal = $"{subtotalValue:0.00} RON";

            var packageAmount = pricing.PackageAmount > 0
                ? $"{pricing.PackageAmount:0.00} RON"
                : "-";

            var monthlyAmount = pricing.MonthlyAmount > 0
                ? $"{pricing.MonthlyAmount:0.00} RON / lună"
                : "-";

            var adjustmentsTotal = act.Items
               .Where(i =>
                   (i.Type == AdditionalActType.AddDiscount ||
                    i.Type == AdditionalActType.IncreasePrice) &&
                   decimal.TryParse(i.NewValue, out _))
               .Sum(i =>
               {
                   var value = decimal.Parse(i.NewValue);
               
                   return i.Type == AdditionalActType.AddDiscount
                       ? -value
                       : value;
               });

            var discount = adjustmentsTotal != 0
                ? $"{adjustmentsTotal:+0.00;-0.00} RON"
                : "-";

            var totalLabel = contract.IsUnlimited
                ? "Total pachet"
                : "Total contract";

            var total = pricing.TotalAmount.HasValue
                ? $"{pricing.TotalAmount.Value:0.00} RON"
                : "-";

            var paymentPlan = "Ratele viitoare se actualizează începând cu data aplicării actului adițional.";

            var changesPrice =
               act.Items.Any(i =>
                   i.Type == AdditionalActType.AddCourse ||
                   i.Type == AdditionalActType.RemoveCourse ||
                   i.Type == AdditionalActType.AddDiscount ||
                   i.Type == AdditionalActType.IncreasePrice
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

                ["Total"] = total,
                ["MonthlyAmount"] = monthlyAmount,

                ["ContractEndDate"] = contract.EndDate.HasValue  ? contract.EndDate.Value.ToString("dd.MM.yyyy") : "Nelimitat",

                ["PricingSection"] = changesPrice
                        ? $@"
                    <h3>IV. PREȚUL ACTUALIZAT AL CONTRACTULUI</h3>
                    <p>Tip contract: {contractType}</p>
                    <p>Subtotal actualizat: {subtotal}</p>
                    <p>Pachet fix actualizat: {packageAmount}</p>
                    <p>Abonament lunar actualizat: {monthlyAmount}</p>
                    <p>Discount / ajustări aplicate: {discount}</p>
                    <p><strong>{totalLabel}: {total}</strong></p>
                    <p>{paymentPlan}</p>"
                        : "",
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
                    .ThenInclude(c => c.Parties)
                .Include(a => a.Contract)
                    .ThenInclude(c => c.Courses)
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
                return response.SetError(ErrorCodes.InvalidParameters, "Actul adițional nu a fost găsit.");

            if (act.Status == AdditionalActStatus.Applied)
                return response.SetError(ErrorCodes.InvalidParameters, "Actul adițional este deja aplicat.");

            if (act.Status != AdditionalActStatus.SignedByClient)
                return response.SetError(ErrorCodes.InvalidParameters, "Actul adițional trebuie semnat de client înainte de aplicare.");

            var contract = act.Contract;

            if (contract.Status != ContractStatus.Active)
                return response.SetError(ErrorCodes.InvalidParameters, "Contractul nu este activ.");

            var studentId = contract.Parties
                .Where(p => p.StudentId.HasValue)
                .Select(p => p.StudentId!.Value)
                .FirstOrDefault();

            if (studentId == 0)
                return response.SetError(ErrorCodes.InvalidParameters, "Cursantul nu a fost găsit.");

            foreach (var item in act.Items)
            {
                switch (item.Type)
                {
                    case AdditionalActType.AddCourse:
                        {
                            if (!item.CourseSessionId.HasValue)
                                return response.SetError(ErrorCodes.InvalidParameters, "ursul este obligatoriu.");

                            var enrollment = await _db.CourseEnrollments
                                .Include(e => e.Session)
                                    .ThenInclude(s => s.Course)
                                .FirstOrDefaultAsync(e =>
                                    e.CourseSessionId == item.CourseSessionId.Value &&
                                    e.StudentId == studentId &&
                                    e.ContractId == null &&
                                    e.IsActive);

                            if (enrollment == null)
                                return response.SetError(
                                    ErrorCodes.InvalidParameters,
                                    "Înscrierea nu a fost găsită sau este deja inclusă în contract."
                                );

                            var alreadyInContractCourses = await _db.ContractCourses
                                .AnyAsync(c =>
                                    c.ContractId == contract.Id &&
                                    c.CourseSessionId == item.CourseSessionId.Value);

                            if (alreadyInContractCourses)
                                return response.SetError(
                                    ErrorCodes.InvalidParameters,
                                    "Cursul există deja în contract."
                                );

                            enrollment.ContractId = contract.Id;

                            _db.ContractCourses.Add(new ContractCourse
                            {
                                ContractId = contract.Id,
                                CourseSessionId = enrollment.Session.Id,
                                CourseNameSnapshot = enrollment.Session.Course.Name,
                                SessionNameSnapshot = enrollment.Session.Title,
                                PriceSnapshot = enrollment.Session.Fee,
                                FeeType = enrollment.Session.FeeType
                            });

                            break;
                        }

                    case AdditionalActType.RemoveCourse:
                        {
                            if (!item.CourseSessionId.HasValue)
                                return response.SetError(ErrorCodes.InvalidParameters, "Cursul există deja în contract.");

                            var existing = await _db.CourseEnrollments
                                .FirstOrDefaultAsync(e =>
                                    e.CourseSessionId == item.CourseSessionId.Value &&
                                    e.ContractId == contract.Id &&
                                    !e.IsActive);

                            var contractCourse = await _db.ContractCourses
                             .FirstOrDefaultAsync(c =>
                                 c.ContractId == contract.Id &&
                                 c.CourseSessionId == item.CourseSessionId.Value);

                            if (contractCourse != null)
                                _db.ContractCourses.Remove(contractCourse);

                            if (existing == null)
                                return response.SetError(ErrorCodes.InvalidParameters, "Cursul eliminat nu a fost găsit în contract.");

                            existing.ContractId = null;

                            break;
                        }

                    case AdditionalActType.ExtendPeriod:
                        {
                            if (!DateTime.TryParse(item.NewValue, out var newDate))
                                return response.SetError(ErrorCodes.InvalidParameters, "Data de sfârșit este invalidă.");

                            if (DateTime.TryParse(item.NewValue, out var newEndDate))
                            {
                                contract.EndDate = newEndDate;
                                contract.IsUnlimited = false;
                            }
                            break;
                        }

                    case AdditionalActType.AddDiscount:
                        {
                            if (!item.CourseSessionId.HasValue)
                                return response.SetError(ErrorCodes.InvalidParameters, "Sesiunea este obligatorie pentru discount.");

                            if (!decimal.TryParse(item.NewValue, out var discount) || discount <= 0)
                                return response.SetError(ErrorCodes.InvalidParameters, "Discountul este invalid.");

                            var contractCourse = await _db.ContractCourses
                                .FirstOrDefaultAsync(c =>
                                    c.ContractId == contract.Id &&
                                    c.CourseSessionId == item.CourseSessionId.Value);

                            if (contractCourse == null)
                                return response.SetError(ErrorCodes.InvalidParameters, "Cursul nu a fost găsit în contract.");

                            contract.PriceAdjustments.Add(new ContractPriceAdjustment
                            {
                                ContractId = contract.Id,
                                CourseSessionId = item.CourseSessionId.Value,
                                Type = PriceAdjustmentType.Discount,
                                Amount = discount,
                                Reason = $"Act adițional {act.ActNumber}",
                                CreatedAtUtc = DateTime.UtcNow
                            });

                            contractCourse.PriceSnapshot = Math.Max(0, contractCourse.PriceSnapshot - discount);

                            break;
                        }

                    case AdditionalActType.IncreasePrice:
                        {
                            if (!item.CourseSessionId.HasValue)
                                return response.SetError(ErrorCodes.InvalidParameters, "Sesiunea este obligatorie pentru majorare.");

                            if (!decimal.TryParse(item.NewValue, out var increase) || increase <= 0)
                                return response.SetError(ErrorCodes.InvalidParameters, "Majorarea este invalidă.");

                            var contractCourse = await _db.ContractCourses
                                .FirstOrDefaultAsync(c =>
                                    c.ContractId == contract.Id &&
                                    c.CourseSessionId == item.CourseSessionId.Value);

                            if (contractCourse == null)
                                return response.SetError(ErrorCodes.InvalidParameters, "Cursul nu a fost găsit în contract.");

                            contract.PriceAdjustments.Add(new ContractPriceAdjustment
                            {
                                ContractId = contract.Id,
                                CourseSessionId = item.CourseSessionId.Value,
                                Type = PriceAdjustmentType.Increase,
                                Amount = increase,
                                Reason = $"Act adițional {act.ActNumber}",
                                CreatedAtUtc = DateTime.UtcNow
                            });

                            contractCourse.PriceSnapshot += increase;

                            break;
                        }

                    default:
                        return response.SetError(ErrorCodes.InvalidParameters, "Tipul modificării nu este acceptat.");
                }
            }

            var pricing = _pricingService.CalculatePricingFromContractCourses(contract);

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
                contract.Installments,
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
                $"Actul adițional {act.ActNumber} a fost aplicat",
                 GetCurrentUser()
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

        private async Task<(List<ContractAdditionalActItem> Items, string Description)> BuildAdditionalActItemsAsync(StudentContract contract, CreateAdditionalActDto dto, int? actId = null)
        {
            var items = new List<ContractAdditionalActItem>();
            var descriptions = new List<string>();

            if (dto.Types == null || !dto.Types.Any())
                throw new InvalidOperationException("Trebuie selectat cel puțin un tip de modificare.");

            var studentId = contract.Parties
                .Where(p => p.StudentId.HasValue)
                .Select(p => p.StudentId!.Value)
                .FirstOrDefault();

            if (studentId == 0)
                throw new InvalidOperationException("Cursantul nu a fost găsit.");

            var selectedSessionIds = dto.AddCourseSessionIds
                .Concat(dto.RemoveCourseSessionIds)
                .Concat(dto.PriceAdjustments?.Select(x => x.CourseSessionId) ?? Enumerable.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            var openActStatuses = new[]
               { 
                AdditionalActStatus.Draft, 
                AdditionalActStatus.Finalized, 
                AdditionalActStatus.SentToClient, 
                AdditionalActStatus.SignedByClient  };

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
                            if (dto.AddCourseSessionIds == null || !dto.AddCourseSessionIds.Any())
                                throw new InvalidOperationException("Cursul este obligatoriu.");

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
                                    throw new InvalidOperationException("Înscrierea nu a fost găsită sau este deja inclusă în contract.");

                                if (enrollment.ContractId != null)
                                    throw new InvalidOperationException("Înscrierea este deja inclusă în contract");

                                var session = enrollment.Session;
                                var price = session.Fee;

                                var typeLabel = session.FeeType == CourseFeeType.Monthly
                                    ? "abonament lunar"
                                    : "pachet fix";

                                items.Add(new ContractAdditionalActItem
                                {
                                    ActId = actId ?? 0,
                                    Type = type,
                                    CourseSessionId = sessionId,
                                    NewValue = price.ToString("0.00")
                                });

                                descriptions.Add(
                                    $"Curs adăugat: {session.Course.Name} - {session.Title} " +
                                    $"({typeLabel}): +{price:0.00} RON"
                                );
                            }

                            break;
                        }

                    case AdditionalActType.RemoveCourse:
                        {
                            if (dto.RemoveCourseSessionIds == null || !dto.RemoveCourseSessionIds.Any())
                                throw new InvalidOperationException("Cursul este obligatoriu.");

                            foreach (var sessionId in dto.RemoveCourseSessionIds.Distinct())
                            {
                                var contractCourse = await _db.ContractCourses
                                    .FirstOrDefaultAsync(c =>
                                        c.ContractId == contract.Id &&
                                        c.CourseSessionId == sessionId);

                                if (contractCourse == null)
                                    throw new InvalidOperationException("Cursul nu a fost găsit în contract.");

                                var existingEnrollment = await _db.CourseEnrollments
                                    .FirstOrDefaultAsync(e =>
                                        e.CourseSessionId == sessionId &&
                                        e.ContractId == contract.Id &&
                                        e.StudentId == studentId &&
                                        !e.IsActive);

                                if (existingEnrollment == null)
                                    throw new InvalidOperationException("Înscrierea nu a fost găsită sau este deja inclusă în contract");

                                var price = contractCourse.PriceSnapshot;

                                var typeLabel = contractCourse.FeeType == CourseFeeType.Monthly
                                    ? "abonament lunar"
                                    : "pachet fix";

                                items.Add(new ContractAdditionalActItem
                                {
                                    ActId = actId ?? 0,
                                    Type = type,
                                    CourseSessionId = sessionId,
                                    NewValue = price.ToString("0.00")
                                });

                                descriptions.Add(
                                    $"Curs eliminat: {contractCourse.CourseNameSnapshot} - {contractCourse.SessionNameSnapshot} " +
                                    $"({typeLabel}): -{price:0.00} RON"
                                );
                            }

                            break;
                        }

                    case AdditionalActType.ExtendPeriod:
                        {
                            if (!dto.NewEndDate.HasValue)
                                throw new InvalidOperationException("Noua dată este obligatorie");

                            if (contract.EndDate.HasValue &&
                                dto.NewEndDate.Value.Date <= contract.EndDate.Value.Date)
                                throw new InvalidOperationException("Noua dată de sfârșit trebuie să fie după data curentă de sfârșit");

                            items.Add(new ContractAdditionalActItem
                            {
                                ActId = actId ?? 0,
                                Type = type,
                                NewValue = dto.NewEndDate.Value.ToString("yyyy-MM-dd")
                            });

                            descriptions.Add($"Perioadă extinsă până la {dto.NewEndDate.Value:dd.MM.yyyy}");

                            break;
                        }

                    case AdditionalActType.AddDiscount:
                        {
                            if (dto.PriceAdjustments == null || !dto.PriceAdjustments.Any())
                                throw new InvalidOperationException("Discountul este obligatoriu.");

                            foreach (var adj in dto.PriceAdjustments)
                            {
                                if (adj.CourseSessionId <= 0 || adj.Amount <= 0)
                                    throw new InvalidOperationException("Discountul este invalid.");

                                var session = await _db.CourseSessions
                                    .Include(s => s.Course)
                                    .FirstOrDefaultAsync(s => s.Id == adj.CourseSessionId);

                                if (session == null)
                                    throw new InvalidOperationException("Sesiunea nu a fost găsită");

                                var typeLabel = session.FeeType == CourseFeeType.Monthly
                                    ? "abonament lunar"
                                    : "pachet fix";

                                items.Add(new ContractAdditionalActItem
                                {
                                    ActId = actId ?? 0,
                                    Type = type,
                                    CourseSessionId = adj.CourseSessionId,
                                    NewValue = adj.Amount.ToString("0.00")
                                });

                                descriptions.Add(
                                    $"Discount aplicat pentru {session.Course.Name} - {session.Title} " +
                                    $"({typeLabel}): -{adj.Amount:0.00} RON"
                                );
                            }

                            break;
                        }

                    case AdditionalActType.IncreasePrice:
                        {
                            if (dto.PriceAdjustments == null || !dto.PriceAdjustments.Any())
                                throw new InvalidOperationException("Majorarea este obligatorie");

                            foreach (var adj in dto.PriceAdjustments)
                            {
                                if (adj.CourseSessionId <= 0 || adj.Amount <= 0)
                                    throw new InvalidOperationException("Majorarea este invalidă.");

                                var session = await _db.CourseSessions
                                    .Include(s => s.Course)
                                    .FirstOrDefaultAsync(s => s.Id == adj.CourseSessionId);

                                if (session == null)
                                    throw new InvalidOperationException("Sesiunea nu a fost găsită");

                                var typeLabel = session.FeeType == CourseFeeType.Monthly
                                    ? "abonament lunar"
                                    : "pachet fix";

                                items.Add(new ContractAdditionalActItem
                                {
                                    ActId = actId ?? 0,
                                    Type = type,
                                    CourseSessionId = adj.CourseSessionId,
                                    NewValue = adj.Amount.ToString("0.00")
                                });

                                descriptions.Add(
                                    $"Majorare preț pentru {session.Course.Name} - {session.Title} " +
                                    $"({typeLabel}): +{adj.Amount:0.00} RON"
                                );
                            }

                            break;
                        }

                    default:
                        throw new InvalidOperationException("Tipul modificării nu este acceptat.");
                }
            }

            return (items, string.Join(" | ", descriptions));
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
}
