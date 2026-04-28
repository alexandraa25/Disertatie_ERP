namespace ERPSystem.Data.Entities
{
    public class CourseReview
    {
        public int Id { get; set; }

        public int CourseSessionId { get; set; }

        public int FeedbackFormId { get; set; }

        // 🔥 GENERAL
        public int Rating { get; set; }

        // 🔥 CURS
        public int? CourseStructureRating { get; set; }
        public int? CoursePaceRating { get; set; }
        public int? MaterialsRating { get; set; }

        // 🔥 PROFESOR
        public int? TeacherClarityRating { get; set; }
        public int? TeacherEngagementRating { get; set; }
        public int? TeacherSupportRating { get; set; }

        // 🔥 TEXT
        public string Comment { get; set; }

        // 🔥 NLP
        public string? Sentiment { get; set; }
        public double? SentimentScore { get; set; }
        public string? Keywords { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AnalyzedAt { get; set; }
    }
}
