namespace ERPSystem.Modules.Contracts.Models
{
    public class PricingResult
    {
        public decimal PackageAmount { get; set; }
        public decimal MonthlyAmount { get; set; }

        public decimal? TotalAmount { get; set; }

        public decimal DiscountTotal { get; set; }
        public int Months { get; set; }

        public decimal TotalDiscountAmount { get; set; }
    }
}
