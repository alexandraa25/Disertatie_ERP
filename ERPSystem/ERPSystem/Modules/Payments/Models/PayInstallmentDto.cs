namespace ERPSystem.Modules.Payments.Models
{
    public class PayInstallmentDto
    {
        public int InstallmentId { get; set; }

        public decimal Amount { get; set; }

        public string Method { get; set; }

        public string? Notes { get; set; }

        public string? Reference { get; set; }
    }
}
