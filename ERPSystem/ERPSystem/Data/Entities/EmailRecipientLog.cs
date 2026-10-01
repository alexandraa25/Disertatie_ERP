namespace ERPSystem.Data.Entities
{
    public class EmailRecipientLog
    {
        public int Id { get; set; }

        public int EmailLogId { get; set; }

        private EmailLog? _emailLog;
        public EmailLog EmailLog
        {
            get => _emailLog ?? throw new InvalidOperationException("Navigation 'EmailLog' has not been loaded.");
            set => _emailLog = value;
        }

        public int? StudentId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string? Name { get; set; }

        public bool IsSent { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime? SentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
