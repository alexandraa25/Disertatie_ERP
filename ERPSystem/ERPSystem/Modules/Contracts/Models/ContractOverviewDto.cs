namespace ERPSystem.Modules.Contracts.Models
{
    public class ContractOverviewDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;

        public string BeneficiaryName { get; set; } = string.Empty;
        public string BeneficiaryEmail { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public decimal? TotalAmount { get; set; }
        public decimal MonthlyAmount { get; set; }

        public string Status { get; set; } = string.Empty;

        public int CoursesCount { get; set; }
        public int InstallmentsCount { get; set; }
        public int PaidInstallmentsCount { get; set; }

        public List<AdditionalActOverviewDto> AdditionalActs { get; set; } = new();
    }

    public class AdditionalActOverviewDto
    {
        public int Id { get; set; }
        public string ActNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public int ItemsCount { get; set; }
    }
}
