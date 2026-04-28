namespace ERPSystem.Modules.Feedback.Models
{
    public class CourseReviewDto
    {
        public int Id { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }

        public int? CourseStructureRating { get; set; }
        public int? CoursePaceRating { get; set; }
        public int? MaterialsRating { get; set; }

        public int? TeacherClarityRating { get; set; }
        public int? TeacherEngagementRating { get; set; }
        public int? TeacherSupportRating { get; set; }

        public string? Sentiment { get; set; }

        public double? SentimentScore { get; set; }

        public string? Keywords { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? AnalyzedAt { get; set; }
    }
}
