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
    public int Id { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = "";
}

public class ContractDetailsDto
{
    public int Id { get; set; }
    public string ContractNumber { get; set; } = default!;
    public int? StudentId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsUnlimited { get; set; }
    public decimal? TotalAmount { get; set; }
    public string DisplayTotal { get; set; } = default!;
    public decimal MonthlyAmount { get; set; }
    public int Installments { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? FinalizedAtUtc { get; set; }

    // semnaturi
    public string? ClientSignature { get; set; }
    public DateTime? ClientSignedAtUtc { get; set; }
    public string? AdminSignature { get; set; }
    public DateTime? AdminSignedAtUtc { get; set; }

    // company
    public string CompanyName { get; set; } = default!;
    public string CompanyAddress { get; set; } = default!;
    public string CompanyCui { get; set; } = default!;
    public string CompanyRegistration { get; set; } = default!;
    public string CompanyIban { get; set; } = default!;
    public string CompanyBank { get; set; } = default!;
    public string CompanyEmail { get; set; } = default!;
    public string CompanyPhone { get; set; } = default!;

    // beneficiar
    public string BeneficiaryName { get; set; } = default!;
    public string BeneficiaryEmail { get; set; } = default!;
    public string BeneficiaryPhone { get; set; } = default!;
    public string BeneficiaryAddress { get; set; } = default!;

    public string ContractBody { get; set; } = default!;

    public List<ContractPartyDto> Parties { get; set; } = new();
    public List<ContractCourseDto> Courses { get; set; } = new();
    public List<ContractDiscountDto> Discounts { get; set; } = new();
    public List<InstallmentDto> InstallmentsList { get; set; } = new();
}

public class ContractPartyDto
{
    public int? StudentId { get; set; }
    public string? StudentName { get; set; }
    public int? GuardianId { get; set; }
    public string? GuardianName { get; set; }
    public string Role { get; set; } = default!;
}

public class ContractDiscountDto
{
    public string Type { get; set; } = default!;
    public decimal Value { get; set; }
    public string Reason { get; set; } = default!;
    public string Scope { get; set; } = default!;
}

public class ContractCourseDto
{
    public int CourseSessionId { get; set; }
    public string CourseName { get; set; } = default!;
    public string SessionName { get; set; } = default!;
    public decimal Price { get; set; }
    public int CourseFeeType { get; set; }
}

public class CreateContractDto
{
    public int? GuardianId { get; set; }
    public int StudentId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsUnlimited { get; set; }
    public int Installments { get; set; }
    public int? MarketingCampaignId { get; set; }
    public List<int> CourseSessionIds { get; set; } = new();
    public List<CreateDiscountDto>? Discounts { get; set; }
}

public class CreateDiscountDto
{
    public string Type { get; set; } = default!;
    public decimal Value { get; set; }
    public string Reason { get; set; } = default!;
    public string Scope { get; set; } = default!;
}

public class UpdateContractDto
{
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsUnlimited { get; set; }
    public int Installments { get; set; }
    public int? MarketingCampaignId { get; set; }
    public List<int> CourseSessionIds { get; set; } = new();
    public List<CreateDiscountDto>? Discounts { get; set; }
}

public class UpdateContractBodyDto
{
    public string ContractBody { get; set; } = default!;
}