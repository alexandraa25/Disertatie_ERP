namespace ERPSystem.Modules.Employees.Models
{
    public class CreateEmployeeRequest
    {
        public string? UserId { get; set; } // optional (poate exista deja user)

        public DateTime HireDate { get; set; }

        public string JobTitle { get; set; }

        public string? EmploymentStatus { get; set; }

        public decimal? Salary { get; set; }

        public string? ContractType { get; set; }

        public string? Notes { get; set; }
    }
}
