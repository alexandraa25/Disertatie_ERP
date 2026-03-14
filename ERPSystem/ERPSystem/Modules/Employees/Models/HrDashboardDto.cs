namespace ERPSystem.Modules.Employees.Models
{
    public class HrDashboardDto
    {
        public int TotalEmployees { get; set; }

        public int ActiveEmployees { get; set; }

        public int TerminatedEmployees { get; set; }

        public int NewHiresThisMonth { get; set; }
    }
}
