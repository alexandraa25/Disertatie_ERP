using ERPSystem.Data.Context;
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

    public async Task<PublicResponse> GetDashboardAsync()
    {
        var response = new PublicResponse(true);

        try
        {
            var activeStudents = await _db.Students
                .Where(s => s.IsActive)
                .CountAsync();

            var activeContracts = await _db.StudentContracts
                .Where(c => c.Status == ContractStatus.Active)
                .CountAsync();

            var totalRevenue = await _db.StudentContracts
                .Where(c =>
                    c.Status == ContractStatus.Active ||
                    c.Status == ContractStatus.Signed)
                .SumAsync(c => c.TotalAmount);

            var monthlyRevenue = await _db.StudentContracts
                .Where(c => c.CreatedAtUtc >= DateTime.UtcNow.AddMonths(-12))
                .GroupBy(c => new
                {
                    c.CreatedAtUtc.Year,
                    c.CreatedAtUtc.Month
                })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync();

            var topCourses = await _db.Courses
    .OrderByDescending(x => x.Enrollments.Count)
    .Take(5)
    .Select(x => new
    {
        x.Name,
        Students = x.Enrollments.Count
    })
    .ToListAsync();

            //var topTeachers = await _db.Users
            //    .Select(t => new
            //    {
            //        Name = t.UserName,
            //        Contracts = _db.StudentContracts.Count(c =>
            //            c.Parties.Any(p => p.GuardianId == t.Id))
            //    })
            //    .OrderByDescending(x => x.Contracts)
            //    .Take(5)
            //    .ToListAsync();

            return response.SetSuccess(new
            {
                activeStudents,
                activeContracts,
                totalRevenue,
                monthlyRevenue, 
                topCourses
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard error");

            return response.SetError("ERROR", ex.Message);
        }
    }
}