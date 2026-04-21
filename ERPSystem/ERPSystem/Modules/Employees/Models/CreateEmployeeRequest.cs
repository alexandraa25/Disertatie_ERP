namespace ERPSystem.Modules.Employees.Models
{
    public class CreateEmployeeFullRequest
    {
        public string Mode { get; set; } // existing | new

        // USER
        public string? UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }

        // EMPLOYEE
        public DateTime HireDate { get; set; }
        public string JobTitle { get; set; }
        public decimal Salary { get; set; }
        public string ContractType { get; set; }
        public string? Notes { get; set; }

        // CONTACT
        public string? PhoneNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }

        // ADDRESS
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }

        // BANK
        public string? IBAN { get; set; }
        public string? BankName { get; set; }

        // DOCUMENTE
        public List<IFormFile>? Files { get; set; }
    }
}
