namespace ERPSystem.Modules.Feedback.Analytics.Models
{
    public class ExternalAnalyticsDto
    {
        public int TotalReviews { get; set; }

        public double AverageRating { get; set; }
        public double PublicPerceptionScore { get; set; }

        public double PositivePercent { get; set; }
        public double NegativePercent { get; set; }
        public double NeutralPercent { get; set; }

        public List<TopicSummaryDto> TopTopics { get; set; } = new();

        public List<TrendDto> Trend { get; set; } = new();

        public List<string> Alerts { get; set; } = new();

        public List<string> Recommendations { get; set; } = new();

        public string Summary { get; set; } = string.Empty;
        public string MainInsight { get; set; } = string.Empty;
    }
}