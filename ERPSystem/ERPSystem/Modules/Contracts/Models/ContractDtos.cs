namespace ERPSystem.Modules.Contracts.Models;

public class ContractListItemDto
{
    public int Id { get; set; }
    public string ContractNumber { get; set; } = null!;

    public string? GuardianName { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public decimal? TotalAmount { get; set; }
    public string DisplayTotal { get; set; } = null!;

    public decimal MonthlyAmount { get; set; }

    public bool IsUnlimited { get; set; }

    public string Status { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}

public class InstallmentDto
{
    public int Id { get; set; } // 🔥 ADD THIS
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
}

public record ContractDetailsDto(
    int Id,
    string ContractNumber,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsUnlimited,
    decimal? TotalAmount,
    string DisplayTotal,
    decimal MonthlyAmount,
    int Installments,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? FinalizedAtUtc,

    // semnături
    string? ClientSignature,
    DateTime? ClientSignedAtUtc,
    string? AdminSignature,
    DateTime? AdminSignedAtUtc,

    // company
    string CompanyName,
    string CompanyAddress,
    string CompanyCui,
    string CompanyRegistration,
    string CompanyIban,
    string CompanyBank,
    string CompanyEmail,
    string CompanyPhone,

    // beneficiar
    string BeneficiaryName,
    string BeneficiaryEmail,
    string BeneficiaryPhone,
    string BeneficiaryAddress,

    string ContractBody,

    List<ContractPartyDto> Parties,
    List<ContractCourseDto> Courses,
    List<ContractDiscountDto> Discounts,
    List<InstallmentDto> InstallmentsList
);
public record ContractPartyDto(
    int? StudentId,
    string? StudentName,
    int? GuardianId,
    string? GuardianName,
    string Role
);

public record ContractDiscountDto(
    string Type,
    decimal Value,
    string Reason,
    string Scope // 🔥 OBLIGATORIU
);

public record ContractCourseDto(
    int CourseSessionId,
    string CourseName,
    string SessionName,
    decimal Price,
    int CourseFeeType // 🔥 ADD
);

public record CreateContractDto(
    int? GuardianId,
    List<int> StudentIds,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsUnlimited,
    int Installments,
    List<int> CourseSessionIds,
    List<CreateDiscountDto>? Discounts
);

public record CreateDiscountDto(
    string Type,
    decimal Value,
    string Reason,
    string Scope // 🔥 OBLIGATORIU
);

public record UpdateContractDto(
    DateTime StartDate,
    DateTime? EndDate,
    bool IsUnlimited,
    int Installments,
    List<int> CourseSessionIds,
    List<CreateDiscountDto>? Discounts
);
public record UpdateContractBodyDto(
    string ContractBody
);

