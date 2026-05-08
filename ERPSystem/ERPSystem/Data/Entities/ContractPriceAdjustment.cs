namespace ERPSystem.Data.Entities
{
    public class ContractPriceAdjustment
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public StudentContract Contract { get; set; } = null!;

        public int? CourseSessionId { get; set; }
        public CourseSession? CourseSession { get; set; }

        public decimal Amount { get; set; }

        public PriceAdjustmentType Type { get; set; }

        public string? Reason { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public enum PriceAdjustmentType
    {
        Increase = 1,
        Discount = 2
    }
}
