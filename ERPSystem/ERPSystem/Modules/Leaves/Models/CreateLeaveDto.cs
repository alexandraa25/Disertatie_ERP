namespace ERPSystem.Modules.Leaves.Models
{
    public class CreateLeaveDto
    {
        public string LeaveType { get; set; } // Vacation / Medical / Unpaid
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
    }
}
