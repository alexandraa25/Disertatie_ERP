namespace ERPSystem.Modules.Employees.Models
{
    public class TerminateEmployeeRequest
    {
        public DateTime TerminationDate { get; set; }

        public string? Reason { get; set; }
    }
}
