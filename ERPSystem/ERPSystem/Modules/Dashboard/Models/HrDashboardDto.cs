namespace ERPSystem.Modules.Dashboard.Models;

public class HrDashboardDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int EmployeesWithoutUser { get; set; }

    public int PendingLeaves { get; set; }
    public int ApprovedLeaves { get; set; }
    public int RejectedLeaves { get; set; }

    public int TotalDocuments { get; set; }
    public decimal AverageSalary { get; set; }

    public List<EmployeeByJobTitleDto> EmployeesByJobTitle { get; set; } = new();
    public List<EmployeeLeaveStatusDto> LeavesByStatus { get; set; } = new();
    public List<EmployeeWithoutUserDto> EmployeesMissingUser { get; set; } = new();
    public List<UpcomingHolidayDto> UpcomingHolidays { get; set; } = new();
}

public class EmployeeByJobTitleDto
{
    public string JobTitle { get; set; } = "";
    public int Count { get; set; }
}

public class EmployeeLeaveStatusDto
{
    public string Status { get; set; } = "";
    public int Count { get; set; }
}

public class EmployeeWithoutUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string JobTitle { get; set; } = "";
}

public class UpcomingHolidayDto
{
    public string Name { get; set; } = "";
    public DateTime Date { get; set; }
}