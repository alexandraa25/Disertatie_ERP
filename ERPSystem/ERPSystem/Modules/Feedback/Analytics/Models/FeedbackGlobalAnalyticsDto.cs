namespace ERPSystem.Modules.Feedback.Analytics.Models
{
    namespace ERPSystem.Modules.Feedback.Analytics.Models
    {
        public class FeedbackGlobalAnalyticsDto
        {
            public int TotalCourseReviews { get; set; }
            public int TotalStudentEvaluations { get; set; }
            public int TotalExternalReviews { get; set; }

            public double AverageCourseRating { get; set; }
            public double AverageStudentEvaluationRating { get; set; }
            public double AverageExternalRating { get; set; }

            public double AveragePublicPerceptionScore { get; set; }
            public double AverageStudentRiskScore { get; set; }

            public List<TopicSummaryDto> TopProblems { get; set; } = new();

            public List<TopTeacherDto> TopTeachers { get; set; } = new();
            public List<TopCourseDto> TopCourses { get; set; } = new();

            public List<TopTeacherDto> TeachersNeedingAttention { get; set; } = new();
            public List<TopCourseDto> CoursesNeedingAttention { get; set; } = new();

            public List<string> Alerts { get; set; } = new();
            public List<string> Recommendations { get; set; } = new();

            public string MainInsight { get; set; } = string.Empty;
            public string Summary { get; set; } = string.Empty;
        }

        public class TopTeacherDto
        {
            public string TeacherUserId { get; set; } = string.Empty;
            public string TeacherName { get; set; } = string.Empty;
            public double AverageRating { get; set; }
            public double AverageTeacherScore { get; set; }
            public double NegativePercent { get; set; }
            public int ReviewsCount { get; set; }
        }

        public class TopCourseDto
        {
            public int CourseSessionId { get; set; }
            public string CourseName { get; set; } = string.Empty;
            public double AverageRating { get; set; }
            public double AverageCourseScore { get; set; }
            public double NegativePercent { get; set; }
            public int ReviewsCount { get; set; }
        }
    }
}
