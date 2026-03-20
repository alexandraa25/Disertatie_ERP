using ERPSystem.Utils.Enums;

namespace ERPSystem.Data.Entities;

public class StudentContract
{
    public int Id { get; set; }

    public string ContractNumber { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsUnlimited { get; set; }

    public decimal TotalAmount { get; set; }

    public int Installments { get; set; } = 1;

    public ContractStatus Status { get; set; }

    public string? ContractBody { get; set; }

    public string? PdfPath { get; set; }

    public bool IsBodyCustomized { get; set; }

    // =========================
    // CLIENT SIGNATURE
    // =========================

    public string? ClientSignature { get; set; }

    public DateTime? ClientSignedAtUtc { get; set; }

    // =========================
    // ADMIN SIGNATURE
    // =========================

    public string? AdminSignature { get; set; }

    public DateTime? AdminSignedAtUtc { get; set; }

    // =========================
    // COMPANY SNAPSHOT
    // =========================

    public string CompanyNameSnapshot { get; set; }

    public string CompanyAddressSnapshot { get; set; }

    public string CompanyCuiSnapshot { get; set; }

    public string CompanyRegistrationSnapshot { get; set; }

    public string CompanyIbanSnapshot { get; set; }

    public string CompanyBankSnapshot { get; set; }

    public string CompanyEmailSnapshot { get; set; }

    public string CompanyPhoneSnapshot { get; set; }

    // =========================
    // BENEFICIARY SNAPSHOT
    // =========================

    public string BeneficiaryNameSnapshot { get; set; }

    public string BeneficiaryEmailSnapshot { get; set; }

    public string BeneficiaryPhoneSnapshot { get; set; }

    public string BeneficiaryAddressSnapshot { get; set; }

 

    // =========================
    // TIMESTAMPS
    // =========================

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? FinalizedAtUtc { get; set; }

    public DateTime? ActivatedAtUtc { get; set; }

    // =========================
    // RELATIONS
    // =========================

    public ICollection<ContractParty> Parties { get; set; } = new List<ContractParty>();

    public ICollection<ContractCourse> Courses { get; set; } = new List<ContractCourse>();

    public ICollection<ContractDiscount> Discounts { get; set; } = new List<ContractDiscount>();

    public ICollection<ContractInstallment> InstallmentsList { get; set; } = new List<ContractInstallment>();

    public ICollection<ContractAdditionalAct> AdditionalActs { get; set; } = new List<ContractAdditionalAct>();
}