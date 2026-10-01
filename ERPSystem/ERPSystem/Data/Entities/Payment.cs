namespace ERPSystem.Data.Entities
{
    public class Payment
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        private StudentContract? _contract;
        public StudentContract Contract
        {
            get => _contract ?? throw new InvalidOperationException("Navigation 'Contract' has not been loaded.");
            set => _contract = value;
        }

        public int? InstallmentId { get; set; }
        public ContractInstallment? Installment { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaidAtUtc { get; set; }

        public string Method { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public string? Reference { get; set; } 

        public string Status { get; set; } = "Completed"; 

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }
    }
}
