namespace ERPSystem.Data.Entities
{
    public class CourseReview
    {
        public int Id { get; set; }

        public int CourseSessionId { get; set; }

        public int FeedbackFormId { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }

        public string? Sentiment { get; set; }

        public double? SentimentScore { get; set; }

        public string? Keywords { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AnalyzedAt { get; set; }
    }
}
