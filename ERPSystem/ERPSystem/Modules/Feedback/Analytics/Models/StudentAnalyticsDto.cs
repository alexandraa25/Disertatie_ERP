namespace ERPSystem.Modules.Feedback.Analytics.Models
{
    public class StudentAnalyticsDto
    {
        public int StudentId { get; set; }

        public double AverageRating { get; set; }
        public double AverageAttendanceScore { get; set; }
        public double AverageBehaviorScore { get; set; }
        public double AverageProgressScore { get; set; }

        public double AverageRiskScore { get; set; }

        public double PositivePercent { get; set; }
        public double NegativePercent { get; set; }
        public double NeutralPercent { get; set; }

        public List<TopicSummaryDto> TopProblems { get; set; } = new();
        public List<TrendDto> Trend { get; set; } = new();

        public List<string> Alerts { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();

        public string Summary { get; set; } = string.Empty;
        public string MainInsight { get; set; } = string.Empty;
    }
}
