namespace ERPSystem.Modules.Feedback.Models
{
    public class SendFeedbackFormsRequest
    {
        public int CourseSessionId { get; set; }

        public List<int> StudentIds { get; set; } = new();
    }

    public class FeedbackFormDetailsDto
    {
        public string CourseName { get; set; }

        public string SessionTitle { get; set; }

        public string TeacherName { get; set; }

        public bool IsCompleted { get; set; }

        public bool IsExpired { get; set; }
    }
    public class SubmitFeedbackRequest
    {
        public string Token { get; set; }

        public int Rating { get; set; }

        public int?   CourseStructureRating { get; set; }
        public int? CoursePaceRating { get; set; }
        public int?     MaterialsRating { get; set; }

        public int? TeacherClarityRating { get; set; }
        public int? TeacherEngagementRating { get; set; }
        public int? TeacherSupportRating { get; set; }

        public string Comment { get; set; }
    }

}
