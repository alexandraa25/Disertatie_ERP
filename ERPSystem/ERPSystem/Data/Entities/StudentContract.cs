using ERPSystem.Data.Entities;
using ERPSystem.Utils.Enums;

public class StudentContract
{
    public int Id { get; set; }

    public string ContractNumber { get; set; } = default!;

    public ContractStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinalizedAtUtc { get; set; }
    public DateTime? SignedAtUtc { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }   // null = nelimitat
    public bool IsUnlimited { get; set; }

    public decimal TotalAmount { get; set; }
    public int Installments { get; set; }

    public string ContractBody { get; set; } = default!;
    public string? PdfPath { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAtUtc { get; set; }

    public string? ClientSignature { get; set; }

    public DateTime? ClientSignedAtUtc { get; set; }

    public ICollection<ContractParty> Parties { get; set; }
        = new List<ContractParty>();

    public ICollection<ContractCourse> Courses { get; set; }
        = new List<ContractCourse>();

    public ICollection<ContractDiscount> Discounts { get; set; }
        = new List<ContractDiscount>();
}