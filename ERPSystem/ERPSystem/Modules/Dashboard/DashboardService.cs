using ERPSystem.Data.Context;
using ERPSystem.Modules.Dashboard.Models;
using ERPSystem.Modules.Employees.Models;
using ERPSystem.Utils.Enums;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Modules.Dashboard;

public class DashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(ApplicationDbContext db, ILogger<DashboardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PublicResponse> GetOverviewAsync()
    {
        var response = new PublicResponse(true);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfNextMonth = startOfMonth.AddMonths(1);

        var dto = new OverviewDashboardDto
        {
            ActiveStudents = await _db.Students
                .CountAsync(s => s.IsActive && !s.IsDeleted),

            ActiveEmployees = await _db.Employees
                .CountAsync(e => e.TerminationDate == null),

            ActiveCourses = await _db.Courses
                .CountAsync(c => c.IsActive && !c.IsDeleted),

            ActiveCourseSessions = await _db.CourseSessions
                .CountAsync(cs => cs.IsActive),

            TotalContracts = await _db.StudentContracts
                .CountAsync(),

            ActiveContracts = await _db.StudentContracts
                .CountAsync(c =>
                    c.Status == ContractStatus.Active ||
                    c.Status == ContractStatus.Finalized ||
                    c.Status == ContractStatus.Signed),

            CurrentMonthRevenue = await _db.Payments
                .Where(p => p.Status == "Completed"
                    && p.PaidAtUtc >= startOfMonth
                    && p.PaidAtUtc < startOfNextMonth)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,

            OverdueAmount = await _db.ContractInstallments
                .Where(i => i.DueDate < now && i.PaidAmount < i.Amount)
                .SumAsync(i => (decimal?)(i.Amount - i.PaidAmount)) ?? 0,

            AverageCourseRating = await _db.CourseReviews
                .AnyAsync()
                    ? await _db.CourseReviews.AverageAsync(r => r.Rating)
                    : 0,

            ActiveCampaigns = await _db.MarketingCampaigns
                .CountAsync(c => c.IsActive && c.StartDate <= now && c.EndDate >= now)
        };

        return response.SetSuccess(dto);
    }

    public async Task<PublicResponse> GetFinancialAsync()
    {
        var response = new PublicResponse(true);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfNextMonth = startOfMonth.AddMonths(1);
        var startLast12Months = startOfMonth.AddMonths(-11);

        var dto = new FinancialDashboardDto
        {
            TotalRevenue = await _db.Payments
                .Where(p => p.Status == "Completed")
                .SumAsync(p => (decimal?)p.Amount) ?? 0,

            CurrentMonthRevenue = await _db.Payments
                .Where(p => p.Status == "Completed"
                    && p.PaidAtUtc >= startOfMonth
                    && p.PaidAtUtc < startOfNextMonth)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,

            OverdueAmount = await _db.ContractInstallments
                .Where(i => i.DueDate < now && i.PaidAmount < i.Amount)
                .SumAsync(i => (decimal?)(i.Amount - i.PaidAmount)) ?? 0,

            PendingInstallmentsAmount = await _db.ContractInstallments
                .Where(i => i.PaidAmount < i.Amount)
                .SumAsync(i => (decimal?)(i.Amount - i.PaidAmount)) ?? 0,

            TotalPayments = await _db.Payments.CountAsync(),

            CompletedPayments = await _db.Payments
                .CountAsync(p => p.Status == "Completed"),

            OverdueInstallments = await _db.ContractInstallments
                .CountAsync(i => i.DueDate < now && i.PaidAmount < i.Amount),

            TotalDiscounts = await _db.ContractDiscounts
                .SumAsync(d => (decimal?)d.Value) ?? 0,

            TotalPriceAdjustments = await _db.ContractPriceAdjustments
                .SumAsync(a => (decimal?)a.Amount) ?? 0
        };

        dto.MonthlyRevenue = await _db.Payments
            .Where(p => p.Status == "Completed" && p.PaidAtUtc >= startLast12Months)
            .GroupBy(p => new { p.PaidAtUtc.Year, p.PaidAtUtc.Month })
            .Select(g => new MonthlyRevenueDto
            {
                Month = g.Key.Month + "/" + g.Key.Year,
                Amount = g.Sum(x => x.Amount)
            })
            .OrderBy(x => x.Month)
            .ToListAsync();

        dto.PaymentsByMethod = await _db.Payments
            .Where(p => p.Status == "Completed")
            .GroupBy(p => p.Method)
            .Select(g => new PaymentMethodDto
            {
                Method = g.Key,
                Count = g.Count(),
                Amount = g.Sum(x => x.Amount)
            })
            .ToListAsync();

        dto.InstallmentsByStatus = await _db.ContractInstallments
            .GroupBy(i => i.Status)
            .Select(g => new InstallmentStatusDto
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                Amount = g.Sum(x => x.Amount)
            })
            .ToListAsync();

        return response.SetSuccess(dto);
    }

    public async Task<PublicResponse> GetEducationAsync()
    {
        var response = new PublicResponse(true);

        var now = DateTime.UtcNow;
        var startLast12Months = new DateTime(now.Year, now.Month, 1).AddMonths(-11);

        var dto = new EducationDashboardDto
        {
            ActiveStudents = await _db.Students
                .CountAsync(s => s.IsActive && !s.IsDeleted),

            ActiveCourses = await _db.Courses
                .CountAsync(c => c.IsActive && !c.IsDeleted),

            ActiveSessions = await _db.CourseSessions
                .CountAsync(s => s.IsActive),

            ActiveEnrollments = await _db.CourseEnrollments
                .CountAsync(e => e.IsActive),

            CompletedFeedbackForms = await _db.FeedbackForms
                .CountAsync(f => f.IsCompleted),

            PendingFeedbackForms = await _db.FeedbackForms
                .CountAsync(f => !f.IsCompleted),

            AverageCourseRating = await _db.CourseReviews.AnyAsync()
                ? await _db.CourseReviews.AverageAsync(r => r.Rating)
                : 0,

            AverageStudentEvaluation = await _db.StudentEvaluations.AnyAsync()
                ? await _db.StudentEvaluations.AverageAsync(e => e.Rating)
                : 0
        };

        dto.EnrollmentsByCourse = await _db.CourseEnrollments
            .Where(e => e.IsActive)
            .GroupBy(e => e.Course.Name)
            .Select(g => new CourseEnrollmentStatsDto
            {
                CourseName = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        dto.RatingByCourse = await (
                from review in _db.CourseReviews
                join form in _db.FeedbackForms
                    on review.FeedbackFormId equals form.Id
                join session in _db.CourseSessions
                    on form.CourseSessionId equals session.Id
                join course in _db.Courses
                    on session.CourseId equals course.Id
                group review by course.Name into g
                select new CourseRatingStatsDto
                {
                    CourseName = g.Key,
                    AverageRating = g.Average(x => x.Rating)
                }
            )
            .OrderByDescending(x => x.AverageRating)
            .Take(10)
            .ToListAsync();

        dto.StudentEvaluationsByMonth = await _db.StudentEvaluations
            .Where(e => e.CreatedAt >= startLast12Months)
            .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
            .Select(g => new StudentEvaluationStatsDto
            {
                Month = g.Key.Month + "/" + g.Key.Year,
                AverageRating = g.Average(x => x.Rating)
            })
            .ToListAsync();

        return response.SetSuccess(dto);
    }

    public async Task<PublicResponse> GetHrAsync()
    {
        var response = new PublicResponse(true);

        var now = DateTime.UtcNow;

        var dto = new HrDashboardDto
        {
            TotalEmployees = await _db.Employees.CountAsync(),

            ActiveEmployees = await _db.Employees
                .CountAsync(e => e.TerminationDate == null),

            EmployeesWithoutUser = await _db.Employees
                .CountAsync(e => e.UserId == null),

            PendingLeaves = await _db.EmployeeLeaves
                .CountAsync(l => l.Status == "Pending"),

            ApprovedLeaves = await _db.EmployeeLeaves
                .CountAsync(l => l.Status == "Approved"),

            RejectedLeaves = await _db.EmployeeLeaves
                .CountAsync(l => l.Status == "Rejected"),

            TotalDocuments = await _db.EmployeeDocuments.CountAsync(),

            AverageSalary = await _db.Employees
                .Where(e => e.Salary.HasValue)
                .AnyAsync()
                    ? await _db.Employees
                        .Where(e => e.Salary.HasValue)
                        .AverageAsync(e => e.Salary!.Value)
                    : 0
        };

        dto.EmployeesByJobTitle = await _db.Employees
            .GroupBy(e => e.JobTitle)
            .Select(g => new EmployeeByJobTitleDto
            {
                JobTitle = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        dto.LeavesByStatus = await _db.EmployeeLeaves
            .GroupBy(l => l.Status)
            .Select(g => new EmployeeLeaveStatusDto
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        dto.EmployeesMissingUser = await _db.Employees
            .Where(e => e.UserId == null)
            .Select(e => new EmployeeWithoutUserDto
            {
                Id = e.Id,
                FullName = e.FirstName + " " + e.LastName,
                Email = e.Email,
                JobTitle = e.JobTitle
            })
            .Take(10)
            .ToListAsync();

        dto.UpcomingHolidays = await _db.PublicHolidays
            .Where(h => h.Date >= now)
            .OrderBy(h => h.Date)
            .Take(5)
            .Select(h => new UpcomingHolidayDto
            {
                Name = h.Name,
                Date = h.Date
            })
            .ToListAsync();

        return response.SetSuccess(dto);
    }
}