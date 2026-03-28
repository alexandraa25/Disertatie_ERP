namespace ERPSystem.Data.Entities
{
    public class ContractInstallment
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public StudentContract Contract { get; set; }

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
