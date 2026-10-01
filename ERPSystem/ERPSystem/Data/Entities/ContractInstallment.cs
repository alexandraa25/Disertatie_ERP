namespace ERPSystem.Data.Entities
{
    public class ContractInstallment
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        private StudentContract? _contract;
        public StudentContract Contract
        {
            get => _contract ?? throw new InvalidOperationException("Navigation 'Contract' has not been loaded.");
            set => _contract = value;
        }

        public DateTime DueDate { get; set; }

        public decimal Amount { get; set; }

        public decimal PaidAmount { get; set; }

        public bool IsPaid => PaidAmount >= Amount;

        public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;
    }

    public enum InstallmentStatus
    {
        Pending,
        Paid,
        Cancelled,
        Expired,
        Suspended
    }
}
