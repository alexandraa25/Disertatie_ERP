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

        [Required]
        public DateTime HireDate { get; set; }

        public DateTime? TerminationDate { get; set; }

        [Required]
        [MaxLength(150)]
        public string JobTitle { get; set; }

        [MaxLength(50)]
        public string? EmploymentStatus { get; set; } // Active / Terminated / Suspended

        public decimal? Salary { get; set; }

        [MaxLength(50)]
        public string? ContractType { get; set; } // FullTime / PartTime / Collaboration

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<EmployeeContract>? Contracts { get; set; }

        public ICollection<EmployeeLeave>? Leaves { get; set; }

        public ICollection<EmployeeDocument>? Documents { get; set; }
    }
}
