namespace ERPSystem.Modules.Feedback.Models
{
    public class AnalyzeReviewRequest
    {
        public string Text { get; set; } = string.Empty;
        public string ReviewType { get; set; } = string.Empty;
    }

    public class AnalyzeReviewResponse
    {
        public string Sentiment { get; set; } = string.Empty;
        public double SentimentScore { get; set; }

        public int PositivePercent { get; set; }
        public int NegativePercent { get; set; }
        public int NeutralPercent { get; set; }

        public string Emotion { get; set; } = string.Empty;
        public string Keywords { get; set; } = string.Empty;

        public double? TeacherScore { get; set; }
        public double? CourseScore { get; set; }
        public double? BehaviorScore { get; set; }

        public double? StudentRiskScore { get; set; }
        public double? BehaviorScoreNlp { get; set; }
        public double? ProgressScoreNlp { get; set; }

        public double? PublicPerceptionScore { get; set; }

        public List<TopicResult> Topics { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }

    public class TopicResult
    {
        public string Name { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
