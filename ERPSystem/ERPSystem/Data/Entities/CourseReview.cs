namespace ERPSystem.Data.Entities
{
    public class CourseReview
    {
        public int Id { get; set; }

        public int CourseSessionId { get; set; }
        public int FeedbackFormId { get; set; }

        public int Rating { get; set; }

        public int? CourseStructureRating { get; set; }
        public int? CoursePaceRating { get; set; }
        public int? MaterialsRating { get; set; }

        public int? TeacherClarityRating { get; set; }
        public int? TeacherEngagementRating { get; set; }
        public int? TeacherSupportRating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public string? Sentiment { get; set; }
        public double? SentimentScore { get; set; }

        public int? PositivePercent { get; set; }
        public int? NegativePercent { get; set; }
        public int? NeutralPercent { get; set; }

        public string? Emotion { get; set; }
        public string? Keywords { get; set; }
        public string? TopicsJson { get; set; }

        public double? TeacherScore { get; set; }
        public double? CourseScore { get; set; }
        public double? BehaviorScore { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AnalyzedAt { get; set; }
    }
}