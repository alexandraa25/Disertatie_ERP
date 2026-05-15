namespace ERPSystem.Modules.Dashboard.Models
{
    public class OverviewDashboardDto
    {
        public int ActiveStudents { get; set; }
        public int ActiveEmployees { get; set; }
        public int ActiveCourses { get; set; }
        public int ActiveCourseSessions { get; set; }

        public int TotalContracts { get; set; }
        public int ActiveContracts { get; set; }

        public decimal CurrentMonthRevenue { get; set; }
        public decimal OverdueAmount { get; set; }

        public double AverageCourseRating { get; set; }

        public int ActiveCampaigns { get; set; }
    }
}
