using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities
{
    public class Employee
    {
        [Key]
        public Guid Id { get; set; }

        // poate fi null dacă angajatul nu are cont
        public string? UserId { get; set; }

        public ApplicationUser? User { get; set; }

       
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Email { get; set; }

        [Required]
        public DateTime HireDate { get; set; }

        public DateTime? TerminationDate { get; set; }

        [Required]
        [MaxLength(150)]
        public string JobTitle { get; set; }

        [MaxLength(50)]
        public string? EmploymentStatus { get; set; } 

        public decimal? Salary { get; set; }

        [MaxLength(50)]
        public string? ContractType { get; set; } 

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int VacationDaysPerYear { get; set; } = 21;

       
        public int CarryOverDays { get; set; } = 0;


        public ICollection<EmployeeLeave>? Leaves { get; set; }

        public ICollection<EmployeeDocument>? Documents { get; set; }

        public EmployeeAddress? Address { get; set; }
        public EmployeeBank? Bank { get; set; }
        public EmployeeContact? Contact { get; set; }
    }
}
