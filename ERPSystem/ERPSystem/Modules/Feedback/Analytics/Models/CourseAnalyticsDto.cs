namespace ERPSystem.Modules.Feedback.Analytics.Models
{
    public class CourseAnalyticsDto
    {
        public int CourseSessionId { get; set; }

        public int TotalReviews { get; set; }

        public double AverageRating { get; set; }
        public double CourseScore { get; set; }
        public double TeacherScore { get; set; }
        public double BehaviorScore { get; set; }

        public double PositivePercent { get; set; }
        public double NegativePercent { get; set; }
        public double NeutralPercent { get; set; }

        public int PositiveReviewsCount { get; set; }
        public int NegativeReviewsCount { get; set; }
        public int NeutralReviewsCount { get; set; }

        public bool NeedsAttention { get; set; }
        public string RiskLevel { get; set; } = "low";

        public List<TopicSummaryDto> TopProblems { get; set; } = new();

        public List<string> Alerts { get; set; } = new();

        public List<string> Recommendations { get; set; } = new();

        public string Summary { get; set; } = string.Empty;

        public List<TrendDto> Trend { get; set; } = new();

        public string MainInsight { get; set; } = string.Empty;
    }
}