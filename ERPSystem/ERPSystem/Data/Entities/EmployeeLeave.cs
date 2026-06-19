using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities
{
    public class EmployeeLeave
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }

        public Employee Employee { get; set; }

        public string LeaveType { get; set; } 

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Status { get; set; } 

        public string? ApprovedBy { get; set; }

        public string? ReasonUpdate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

