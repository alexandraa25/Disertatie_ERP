using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities
{
    public class EmployeeContract
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }

        public Employee Employee { get; set; }

        public string ContractType { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public decimal Salary { get; set; }

        public string? FileUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
