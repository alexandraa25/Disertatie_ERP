namespace ERPSystem.Modules.Leaves.Models
{
    public class GetLeavesQuery
    {
        public string? Status { get; set; }      
        public string? LeaveType { get; set; } 
        public Guid? EmployeeId { get; set; }

        public DateTime? StartFrom { get; set; }
        public DateTime? StartTo { get; set; }

        public string? Search { get; set; }

        public string? SortBy { get; set; } = "StartDate";
        public string? SortOrder { get; set; } = "desc";   

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
