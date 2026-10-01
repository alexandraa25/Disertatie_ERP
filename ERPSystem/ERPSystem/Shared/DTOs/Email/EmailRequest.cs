using System.Net.Mail;

namespace ERPSystem.Shared.DTOs.Email
{
    public class EmailRequest
    {
        public EmailAddress From { get; set; } = new();
        public List<EmailAddress> To { get; set; } = [];
        public string Subject { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
    }

    public class EmailAddress
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
