namespace ERPSystem.Modules.Employees.Models
{
    public class EmployeeListRequest
    {
        public string? Search { get; set; }

        public string? EmploymentStatus { get; set; }
        public string? ContractType { get; set; }
        public string? JobTitle { get; set; }

        public DateTime? HireDateFrom { get; set; }
        public DateTime? HireDateTo { get; set; }

        public string? SortBy { get; set; } = "hireDate";
        public string? SortDirection { get; set; } = "desc";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
