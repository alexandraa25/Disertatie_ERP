namespace ERPSystem.Modules.Feedback.Models
{
    public class CreateExternalReviewRequest
    {
        public string Source { get; set; } = string.Empty;

        public string TargetType { get; set; } = "General";
        public string? TargetId { get; set; }

        public int? Rating { get; set; }

        public string Comment { get; set; } = string.Empty;
    }

    public class ExternalReviewDto
    {
        public int Id { get; set; }

        public string Source { get; set; } = string.Empty;

        public string TargetType { get; set; } = string.Empty;
        public string? TargetId { get; set; }

        public int? Rating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public string? Sentiment { get; set; }
        public double? SentimentScore { get; set; }

        public int? PositivePercent { get; set; }
        public int? NegativePercent { get; set; }
        public int? NeutralPercent { get; set; }

        public string? Emotion { get; set; }
        public string? Keywords { get; set; }
        public string? TopicsJson { get; set; }

        public double? PublicPerceptionScore { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? AnalyzedAt { get; set; }
    }

    public class ExternalReviewFilterRequest
    {
        public string? TargetType { get; set; }
        public string? TargetId { get; set; }
        public string? Source { get; set; }
    }
}
