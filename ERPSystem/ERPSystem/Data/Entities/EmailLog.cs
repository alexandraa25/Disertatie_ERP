namespace ERPSystem.Data.Entities
{
    public class EmailLog
    {
        public int Id { get; set; }

        public string Type { get; set; }

        public int? ReferenceId { get; set; }

        public string Subject { get; set; }

        public string HtmlContent { get; set; }

        public string? RecipientMode { get; set; }

        public int TotalRecipients { get; set; }

        public int SentCount { get; set; }

        public int FailedCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SentAt { get; set; }

        public ICollection<EmailRecipientLog> Recipients { get; set; } = new List<EmailRecipientLog>();
    }
}
