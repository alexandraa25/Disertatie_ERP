namespace ERPSystem.Modules.Admin.Models
{
    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }

        public int ActiveUsers { get; set; }

        public int InactiveUsers { get; set; }

        public int AdminUsers { get; set; }

        public List<CompanyUserDto> LatestUsers { get; set; }

        public List<CompanyUserDto> Users { get; set; }
    }
}
