using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities
{
    public class EmployeeDocument
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }

        public Employee Employee { get; set; }

        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ContentType { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string? UploadedBy { get; set; }
    }
}

