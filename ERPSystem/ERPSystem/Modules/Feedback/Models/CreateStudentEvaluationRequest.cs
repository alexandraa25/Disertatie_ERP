namespace ERPSystem.Modules.Feedback.Models
{
    public class CreateStudentEvaluationRequest
    {
        public int StudentId { get; set; }

        public int CourseSessionId { get; set; }

        public int Rating { get; set; }

        public int? AttendanceScore { get; set; }

        public int? BehaviorScore { get; set; }

        public int? ProgressScore { get; set; }

        public string? Comment { get; set; }
    }

    public class StudentEvaluationDto
    {
        public int Id { get; set; }

        public int StudentId { get; set; }

        public string StudentName { get; set; }

        public int CourseSessionId { get; set; }

        public string TeacherName { get; set; }

        public int Rating { get; set; }

        public int? AttendanceScore { get; set; }

        public int? BehaviorScore { get; set; }

        public int? ProgressScore { get; set; }

        public string? Comment { get; set; }

        public string? Sentiment { get; set; }

        public double? SentimentScore { get; set; }

        public string? Keywords { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
