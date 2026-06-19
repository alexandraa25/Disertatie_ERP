using System.Net.Mail;

namespace ERPSystem.Shared.DTOs.Email
{
    public class EmailRequest
    {
        public EmailAddress From { get; set; }
        public List<EmailAddress> To { get; set; }
        public string Subject { get; set; }
        public string Html { get; set; }
    }

    public class EmailAddress
    {
        public string Email { get; set; }
        public string Name { get; set; }  
    }
}
