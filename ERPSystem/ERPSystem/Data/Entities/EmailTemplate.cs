namespace ERPSystem.Data.Entities
{
    public class EmailTemplate
    {
        public int Id { get; set; }

        public string TemplateCode { get; set; } 

        public string Subject { get; set; }

        public string HtmlContent { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
