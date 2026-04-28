namespace ERPSystem.Data.Entities
{
    public class FeedbackForm
    {
        public int Id { get; set; }

        public int? StudentId { get; set; }

        public int CourseSessionId { get; set; }

        public string Token { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}
