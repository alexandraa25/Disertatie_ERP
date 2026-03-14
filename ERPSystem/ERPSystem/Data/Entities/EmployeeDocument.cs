using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities
{
    public class EmployeeDocument
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }

        public Employee Employee { get; set; }

        public string DocumentType { get; set; } // CV / Contract / Diploma

        public string FileUrl { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string? UploadedBy { get; set; }
    }
}

