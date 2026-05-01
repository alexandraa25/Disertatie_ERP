namespace ERPSystem.Data.Entities
{
    public class StudentEvaluation
    {
        public int Id { get; set; }

        public int StudentId { get; set; }
        public int CourseSessionId { get; set; }

        public string TeacherUserId { get; set; } = string.Empty;

        public int Rating { get; set; }

        public int? AttendanceScore { get; set; }
        public int? BehaviorScore { get; set; }
        public int? ProgressScore { get; set; }

        public string? Comment { get; set; }

        public string? Sentiment { get; set; }
        public double? SentimentScore { get; set; }

        public int? PositivePercent { get; set; }
        public int? NegativePercent { get; set; }
        public int? NeutralPercent { get; set; }

        public string? Emotion { get; set; }
        public string? Keywords { get; set; }
        public string? TopicsJson { get; set; }

        public double? StudentRiskScore { get; set; }
        public double? BehaviorScoreNlp { get; set; }
        public double? ProgressScoreNlp { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AnalyzedAt { get; set; }
    }
}