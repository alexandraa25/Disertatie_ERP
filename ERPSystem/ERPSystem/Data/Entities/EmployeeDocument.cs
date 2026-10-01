using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities
{
    public class EmployeeDocument
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }

        private Employee? _employee;
        public Employee Employee
        {
            get => _employee ?? throw new InvalidOperationException("Navigation 'Employee' has not been loaded.");
            set => _employee = value;
        }

        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public string? UploadedBy { get; set; }
    }
}

