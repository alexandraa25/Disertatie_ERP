using ERPSystem.Utils.Enums;

namespace ERPSystem.Data.Entities;

public class StudentContract
{
    public int Id { get; set; }

    public string ContractNumber { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsUnlimited { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal MonthlyAmount { get; set; }

    public int Installments { get; set; } = 1;

    public ContractStatus Status { get; set; }

    public string? ContractBody { get; set; }

    public string? PdfPath { get; set; }

    public bool IsBodyCustomized { get; set; }


    public string? ClientSignature { get; set; }

    public DateTime? ClientSignedAtUtc { get; set; }


    public string? AdminSignature { get; set; }

    public DateTime? AdminSignedAtUtc { get; set; }

  
    public string CompanyNameSnapshot { get; set; } = string.Empty;

    public string CompanyAddressSnapshot { get; set; } = string.Empty;

    public string CompanyCuiSnapshot { get; set; } = string.Empty;

    public string CompanyRegistrationSnapshot { get; set; } = string.Empty;

    public string CompanyIbanSnapshot { get; set; } = string.Empty;

    public string CompanyBankSnapshot { get; set; } = string.Empty;

    public string CompanyEmailSnapshot { get; set; } = string.Empty;

    public string CompanyPhoneSnapshot { get; set; } = string.Empty;


    public string BeneficiaryNameSnapshot { get; set; } = string.Empty;

    public string BeneficiaryEmailSnapshot { get; set; } = string.Empty;

    public string BeneficiaryPhoneSnapshot { get; set; } = string.Empty;

    public string BeneficiaryAddressSnapshot { get; set; } = string.Empty;

 

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public DateTime? FinalizedAtUtc { get; set; }

    public DateTime? ActivatedAtUtc { get; set; }


    public ICollection<ContractParty> Parties { get; set; } = new List<ContractParty>();

    public ICollection<ContractCourse> Courses { get; set; } = new List<ContractCourse>();

    public ICollection<ContractDiscount> Discounts { get; set; } = new List<ContractDiscount>();

    public ICollection<ContractInstallment> InstallmentsList { get; set; } = new List<ContractInstallment>();

    public ICollection<ContractAdditionalAct> AdditionalActs { get; set; } = new List<ContractAdditionalAct>();
    public ICollection<ContractPriceAdjustment> PriceAdjustments { get; set; } = new List<ContractPriceAdjustment>();
}