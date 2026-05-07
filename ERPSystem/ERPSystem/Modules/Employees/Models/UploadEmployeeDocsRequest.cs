namespace ERPSystem.Modules.Employees.Models
{
    public class UploadEmployeeDocsRequest
    {
        public Guid EmployeeId { get; set; }

        public IFormFile? File { get; set; }

        public string? DocumentType { get; set; }
    }
}
