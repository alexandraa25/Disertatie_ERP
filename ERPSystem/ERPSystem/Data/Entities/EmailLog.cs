namespace ERPSystem.Data.Entities
{
    public class EmailLog
    {
        public int Id { get; set; }

        public string Type { get; set; } = string.Empty;

        public int? ReferenceId { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string HtmlContent { get; set; } = string.Empty;

        public string? RecipientMode { get; set; }

        public int TotalRecipients { get; set; }

        public int SentCount { get; set; }

        public int FailedCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SentAt { get; set; }

        public ICollection<EmailRecipientLog> Recipients { get; set; } = new List<EmailRecipientLog>();
    }
}
