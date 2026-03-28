namespace ERPSystem.Data.Entities
{
    public class Payment
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public StudentContract Contract { get; set; }

        public int? InstallmentId { get; set; }
        public ContractInstallment? Installment { get; set; }

        public decimal Amount { get; set; }

        public DateTime PaidAtUtc { get; set; }

        public string Method { get; set; }

        public string? Notes { get; set; }

        public string? Reference { get; set; } // nr chitanță / OP

        public string Status { get; set; } = "Completed"; //Completed, Pending, Failed, Refunded

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }
    }
}
