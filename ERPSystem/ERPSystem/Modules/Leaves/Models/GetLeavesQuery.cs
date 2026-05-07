namespace ERPSystem.Modules.Leaves.Models
{
    public class GetLeavesQuery
    {
        public string? Status { get; set; }      // Pending / Approved / Rejected
        public string? LeaveType { get; set; }  // Vacation / Sick / Unpaid
        public Guid? EmployeeId { get; set; }

        public DateTime? StartFrom { get; set; }
        public DateTime? StartTo { get; set; }

        public string? Search { get; set; }

        public string? SortBy { get; set; } = "StartDate"; // StartDate, EndDate
        public string? SortOrder { get; set; } = "desc";   // asc / desc

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
