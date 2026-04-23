namespace ERPSystem.Modules.Employees.Models
{
    public class UploadEmployeeDocsRequest
    {
        public Guid EmployeeId { get; set; }

        public List<string> DocumentTypes { get; set; } = new();
        public List<IFormFile> Files { get; set; } = new();
    }
}
