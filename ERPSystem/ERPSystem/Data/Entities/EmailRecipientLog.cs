namespace ERPSystem.Data.Entities
{
    public class EmailRecipientLog
    {
        public int Id { get; set; }

        public int EmailLogId { get; set; }

        public EmailLog EmailLog { get; set; }

        public int? StudentId { get; set; }

        public string Email { get; set; }

        public string? Name { get; set; }

        public bool IsSent { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime? SentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
