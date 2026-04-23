namespace ERPSystem.Modules.Employees.Models
{
    public class TerminateEmployeeRequest
    {
        public DateTime TerminationDate { get; set; }

        public IFormFile? File { get; set; }

        public string? DocumentType { get; set; }

        public string? Reason { get; set; }
    }
}
