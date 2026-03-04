using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Enums;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Modules.Contracts;

public class ContractsService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ContractsService> _logger;

    public ContractsService(
        ApplicationDbContext db,
        ILogger<ContractsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // =========================
    // CREATE CONTRACT
    // =========================
    public async Task<PublicResponse> CreateAsync(CreateContractDto dto)
    {
        var response = new PublicResponse(true);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            // =============================
            // BASIC VALIDATION
            // =============================
            if (!dto.StudentIds.Any() || !dto.CourseSessionIds.Any())
                return response.SetError(ErrorCodes.InvalidParameters, "Invalid parameters");

            if (!dto.IsUnlimited && dto.EndDate == null)
                return response.SetError(ErrorCodes.InvalidParameters, "EndDate required");

            if (!dto.IsUnlimited && dto.EndDate < dto.StartDate)
                return response.SetError(ErrorCodes.InvalidParameters, "Invalid period");

            // =============================
            // LOAD STUDENTS
            // =============================
            var students = await _db.Students
                .Where(x => dto.StudentIds.Contains(x.Id))
                .ToListAsync();

            if (students.Count != dto.StudentIds.Count)
                return response.SetError(ErrorCodes.InvalidParameters, "Invalid students");

            // =============================
            // ACTIVE CONTRACT VALIDATION
            // =============================
            var hasActiveContract = await _db.StudentContracts
                .Include(c => c.Parties)
                .AnyAsync(c =>
                    c.Status == ContractStatus.Active &&
                    c.Parties.Any(p =>
                        p.StudentId.HasValue && dto.StudentIds.Contains(p.StudentId.Value)));

            if (hasActiveContract)
                return response.SetError(
                    ErrorCodes.InvalidParameters,
                    "Student already has an active contract");

            // =============================
            // GUARDIAN VALIDATION
            // =============================
            Guardian? guardian = null;

            if (dto.GuardianId.HasValue)
            {
                guardian = await _db.Guardians
                    .FirstOrDefaultAsync(x => x.Id == dto.GuardianId.Value);

                if (guardian is null)
                    return response.SetError(ErrorCodes.InvalidParameters, "Guardian not found");
            }

            var hasMinor = students.Any(s => s.IsMinor);

            if (hasMinor)
            {
                if (guardian is null)
                    return response.SetError(ErrorCodes.InvalidParameters,
                        "Guardian required for minor student");

                var guardianLinked = await _db.StudentGuardians
                    .AnyAsync(sg =>
                        dto.StudentIds.Contains(sg.StudentId) &&
                        sg.GuardianId == guardian.Id);

                if (!guardianLinked)
                    return response.SetError(ErrorCodes.InvalidParameters,
                        "Guardian not linked to selected student");
            }

            // =============================
            // LOAD SESSIONS
            // =============================
            var sessions = await _db.CourseSessions
                .Include(x => x.Course)
                .Where(x => dto.CourseSessionIds.Contains(x.Id))
                .ToListAsync();

            if (sessions.Count != dto.CourseSessionIds.Count)
                return response.SetError(ErrorCodes.InvalidParameters, "Invalid sessions");

            // =============================
            // CAPACITY VALIDATION
            // =============================
            foreach (var session in sessions)
            {
                if (session.Capacity.HasValue)
                {
                    var enrolled = await _db.CourseEnrollments
                        .CountAsync(e =>
                            e.CourseSessionId == session.Id &&
                            e.IsActive);

                    if (enrolled >= session.Capacity.Value)
                    {
                        return response.SetError(
                            ErrorCodes.InvalidParameters,
                            $"Session {session.Title} is full");
                    }
                }
            }

            // =============================
            // CREATE CONTRACT
            // =============================
            var contract = new StudentContract
            {
                ContractNumber = GenerateContractNumber(),
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsUnlimited = dto.IsUnlimited,
                Installments = dto.Installments <= 0 ? 1 : dto.Installments,
                Status = ContractStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            // Parties
            if (guardian != null)
            {
                contract.Parties.Add(new ContractParty
                {
                    GuardianId = guardian.Id,
                    Role = ContractPartyRole.Guardian
                });
            }

            foreach (var student in students)
            {
                contract.Parties.Add(new ContractParty
                {
                    StudentId = student.Id,
                    Role = ContractPartyRole.Student
                });
            }

            // Courses snapshot
            foreach (var session in sessions)
            {
                contract.Courses.Add(new ContractCourse
                {
                    CourseSessionId = session.Id,
                    CourseNameSnapshot = session.Course.Name,
                    SessionNameSnapshot = session.Title,
                    PriceSnapshot = session.Fee
                });
            }

            // Discounts
            if (dto.Discounts != null)
            {
                foreach (var d in dto.Discounts)
                {
                    contract.Discounts.Add(new ContractDiscount
                    {
                        Type = Enum.Parse<DiscountType>(d.Type),
                        Value = d.Value,
                        Reason = d.Reason
                    });
                }
            }

            // =============================
            // CALCULATE TOTAL
            // =============================
            contract.TotalAmount = CalculateTotal(contract);

            // =============================
            // GENERATE INSTALLMENTS
            // =============================
            //if (contract.Installments > 1)
            //{
            //    var installmentAmount = Math.Round(
            //        contract.TotalAmount / contract.Installments, 2);

            //    for (int i = 0; i < contract.Installments; i++)
            //    {
            //        contract.InstallmentsList.Add(new ContractInstallment
            //        {
            //            DueDate = contract.StartDate.AddMonths(i),
            //            Amount = installmentAmount,
            //            IsPaid = false
            //        });
            //    }
            //}

            // =============================
            // GENERATE BODY
            // =============================
            contract.ContractBody = GenerateContractBody(contract, guardian, students);

            _db.StudentContracts.Add(contract);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            return response.SetCreated(new { contract.Id });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "CreateContract failed");
            return response.SetError(ErrorCodes.InternalServerError, "Internal error");
        }
    }

    // =========================
    // UPDATE (Draft only)
    // =========================
    public async Task<PublicResponse> UpdateAsync(int id, UpdateContractDto dto)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .Include(c => c.Courses)
            .Include(c => c.Discounts)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        if (contract.Status != ContractStatus.Draft)
            return response.SetError(ErrorCodes.InvalidParameters, "Only draft editable");

        contract.StartDate = dto.StartDate;
        contract.EndDate = dto.EndDate;
        contract.IsUnlimited = dto.IsUnlimited;
        contract.Installments = dto.Installments;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        contract.Courses.Clear();
        contract.Discounts.Clear();

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    // =========================
    // FINALIZE
    // =========================
    public async Task<PublicResponse> FinalizeAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts.FindAsync(id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        if (contract.Status != ContractStatus.Draft)
            return response.SetError(ErrorCodes.InvalidParameters, "Only Draft can finalize");

        if (string.IsNullOrWhiteSpace(contract.ContractBody))
            return response.SetError(ErrorCodes.InvalidParameters, "Contract body empty");

        contract.Status = ContractStatus.Finalized;
        contract.FinalizedAtUtc = DateTime.UtcNow;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    // =========================
    // SIGN
    // =========================
    public async Task<PublicResponse> SignAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts.FindAsync(id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        if (contract.Status != ContractStatus.Finalized)
            return response.SetError(ErrorCodes.InvalidParameters, "Finalize first");

        contract.Status = ContractStatus.Signed;
        contract.SignedAtUtc = DateTime.UtcNow;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    // =========================
    // ACTIVATE
    // =========================
    public async Task<PublicResponse> ActivateAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts.FindAsync(id); 

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        if (contract.Status != ContractStatus.Signed)
            return response.SetError(ErrorCodes.InvalidParameters,
                "Contract must be signed before activation");
        if (!contract.IsUnlimited && contract.EndDate == null)
            return response.SetError(ErrorCodes.InvalidParameters,
                "EndDate required if not unlimited");

        if (!contract.IsUnlimited && contract.EndDate < contract.StartDate)
            return response.SetError(ErrorCodes.InvalidParameters,
                "Invalid period");

        contract.Status = ContractStatus.Active;
        contract.ActivatedAtUtc = DateTime.UtcNow;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    // =========================
    // LIST BY STUDENT
    // =========================
    public async Task<PublicResponse> ListByStudentAsync(int studentId)
    {
        var response = new PublicResponse(true);

        var contracts = await _db.StudentContracts
     .AsNoTracking()
     .Include(c => c.Parties)
         .ThenInclude(p => p.Guardian)
     .Where(c => c.Parties.Any(p =>
         p.StudentId == studentId &&
         p.Role == ContractPartyRole.Student))
     .Select(c => new ContractListItemDto(
         c.Id,
         c.ContractNumber,
         c.Parties
             .Where(p => p.Role == ContractPartyRole.Guardian)
             .Select(p => p.Guardian.FirstName + " " + p.Guardian.LastName)
             .FirstOrDefault(),
         c.StartDate,
         c.EndDate,
         c.TotalAmount,
         c.Status.ToString(),
         c.CreatedAtUtc
     ))
     .ToListAsync();

        return response.SetSuccess(contracts);
    }

    // =========================
    // GET BY ID
    // =========================
    public async Task<PublicResponse> GetByIdAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .AsNoTracking()
            .Include(c => c.Courses)
            .Include(c => c.Discounts)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        var dto = new
        {
            contract.Id,
            contract.ContractNumber,
            Status = contract.Status.ToString(),
            contract.StartDate,
            contract.EndDate,
            contract.IsUnlimited,
            contract.TotalAmount,
            contract.Installments,
            contract.ContractBody,
            Courses = contract.Courses.Select(c => new
            {
                c.CourseNameSnapshot,
                c.SessionNameSnapshot,
                c.PriceSnapshot
            }),
            Discounts = contract.Discounts.Select(d => new
            {
                Type = d.Type.ToString(),
                d.Value,
                d.Reason
            })
        };

        return response.SetSuccess(dto);
    }

    // =========================
    // HELPERS
    // =========================
    private decimal CalculateTotal(StudentContract contract)
    {
        var total = contract.Courses.Sum(c => c.PriceSnapshot);

        foreach (var discount in contract.Discounts)
        {
            if (discount.Type == DiscountType.Percentage)
                total -= total * (discount.Value / 100m);
            else
                total -= discount.Value;
        }

        return total < 0 ? 0 : total;
    }

    private string GenerateContractNumber()
    {
        return $"CTR-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString()[..6]}";
    }

    private string GenerateContractBody(
     StudentContract contract,
     Guardian? guardian,
     List<ERPSystem.Data.Entities.Student> students)
    {
        var studentList = string.Join(", ", students.Select(s => s.FullName));

        var coursesList = string.Join("\n",
            contract.Courses.Select(c =>
                $"- {c.CourseNameSnapshot} ({c.SessionNameSnapshot}) – {c.PriceSnapshot} RON"));

        var beneficiar = guardian != null
       ? $@"{guardian.FirstName} {guardian.LastName}
Email: {guardian.Email}
Telefon: {guardian.Phone}"
       : studentList;


        // 🔥 DISCOUNT SECTION
        var discountSection = "Nu există discounturi aplicate.";

        if (contract.Discounts.Any())
        {
            discountSection = string.Join("\n",
                contract.Discounts.Select(d =>
                    $"- {d.Type} | Valoare: {d.Value} | Motiv: {d.Reason}"));
        }

        var subtotal = contract.Courses.Sum(c => c.PriceSnapshot);
        var totalDiscount = subtotal - contract.TotalAmount;

        return $@"
CONTRACT DE PRESTĂRI DE SERVICII
Nr. {contract.ContractNumber}
Încheiat astăzi {DateTime.UtcNow:dd.MM.yyyy}

I. PĂRŢILE CONTRACTANTE

1.1. S.C. GLOBAL L SRL,
...

1.2. {beneficiar}, în calitate de BENEFICIAR.

II. OBIECTUL CONTRACTULUI

Servicii educaționale:

{coursesList}

Cursanți:
{studentList}

III. DURATA CONTRACTULUI

{(contract.IsUnlimited
    ? "Perioadă nedeterminată"
    : $"{contract.StartDate:dd.MM.yyyy} - {contract.EndDate:dd.MM.yyyy}")}

IV. PREŢUL CONTRACTULUI

Subtotal (fără discount): {subtotal:F2} RON

Discounturi aplicate:
{discountSection}

Reducere totală: {totalDiscount:F2} RON

Valoare finală contract: {contract.TotalAmount:F2} RON

Număr rate: {contract.Installments}

Plata se efectuează prin transfer bancar sau numerar,
conform condițiilor stabilite de comun acord.

V. OBLIGAŢIILE PĂRŢILOR
5.1. Prestatorul se obligă: - să furnizeze serviciile educaționale conform programului stabilit; - să asigure calitatea actului educațional.
5.2. Beneficiarul se obligă: - să achite contravaloarea serviciilor; - să respecte programul cursurilor.
VI. ÎNCETAREA CONTRACTULUI
Contractul poate înceta prin acordul părților sau prin notificare scrisă din partea uneia dintre părți.

PRESTATOR                                  BENEFICIAR

GLOBAL LEARNING SRL                        {beneficiar}
";
    }

    public async Task<PublicResponse> UpdateBodyAsync(int id, UpdateContractBodyDto dto)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts.FindAsync(id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Contract not found");

        if (contract.Status != ContractStatus.Draft)
            return response.SetError(ErrorCodes.InvalidParameters,
                "Contract can be edited only in Draft");

        contract.ContractBody = dto.ContractBody;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> CancelAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts.FindAsync(id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        contract.Status = ContractStatus.Cancelled;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> GeneratePdfAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts.FindAsync(id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        var folder = Path.Combine("wwwroot", "contracts");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var filePath = Path.Combine(folder, $"{contract.ContractNumber}.txt");

        await File.WriteAllTextAsync(filePath, contract.ContractBody);

        return response.SetSuccess(new { path = filePath });
    }
    public async Task<PublicResponse> GetLatestByStudentAsync(int studentId)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .Include(c => c.Parties)
            .Where(c => c.Parties.Any(p =>
                p.Role == ContractPartyRole.Student &&
                p.StudentId.HasValue &&
                p.StudentId.Value == studentId))
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new
            {
                c.Id,
                Status = c.Status.ToString()
            })
            .FirstOrDefaultAsync();

        return response.SetSuccess(contract);
    }




}