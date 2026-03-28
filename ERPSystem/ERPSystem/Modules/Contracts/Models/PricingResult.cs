namespace ERPSystem.Modules.Contracts.Models
{
    public class PricingResult
    {
        public decimal PackageTotal { get; set; }
        public decimal SubscriptionMonthly { get; set; }
        public int? SubscriptionMonths { get; set; }
        public decimal? SubscriptionTotal { get; set; }
        public decimal? Total { get; set; }
    }
}
