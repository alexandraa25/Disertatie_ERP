namespace ERPSystem.Modules.Employees.Models
{
    public class EmployeeDto
    {
        public Guid Id { get; set; }

        public string? UserId { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string JobTitle { get; set; }

        public DateTime HireDate { get; set; }

        public DateTime? TerminationDate { get; set; }

        public string? EmploymentStatus { get; set; }

        public decimal? Salary { get; set; }

        public string? ContractType { get; set; }
    }
}
