namespace ERPSystem.Modules.Feedback.Analytics.Models
{
    public class TrendDto
    {
        public string Month { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public double PositivePercent { get; set; }
        public double NegativePercent { get; set; }
        public int ReviewCount { get; set; }
    }
}
