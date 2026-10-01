using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Data.Entities
{
    public class EmployeeLeave
    {
        [Key]
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }

        private Employee? _employee;
        public Employee Employee
        {
            get => _employee ?? throw new InvalidOperationException("Navigation 'Employee' has not been loaded.");
            set => _employee = value;
        }

        public string LeaveType { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? ApprovedBy { get; set; }

        public string? ReasonUpdate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

