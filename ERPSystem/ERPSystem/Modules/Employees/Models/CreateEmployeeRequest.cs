using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Modules.Employees.Models
{
    public class CreateEmployeeFullRequest
    {
        public string Mode { get; set; } 

        public string? UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }

        public DateTime HireDate { get; set; }
        public string JobTitle { get; set; }
        public decimal Salary { get; set; }
        public string ContractType { get; set; }
        public string? Notes { get; set; }

        public string? PhoneNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }

        public string? Street { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }

        public string? IBAN { get; set; }
        public string? BankName { get; set; }

        [FromForm(Name = "Files")]
        public IFormFile[] Files { get; set; } = Array.Empty<IFormFile>();

        [FromForm(Name = "DocumentTypes")]
        public string[] DocumentTypes { get; set; } = Array.Empty<string>();
    }
}
