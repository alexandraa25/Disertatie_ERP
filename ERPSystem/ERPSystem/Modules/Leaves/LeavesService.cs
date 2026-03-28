using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Modules.Leaves.Models;
using ERPSystem.Modules.Payments.Models;
using ERPSystem.Shared.BusinessLogic;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Enums;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Modules.Payments
{
    public class LeavesService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public LeavesService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
        {
            _context = db;
            _httpContextAccessor = httpContextAccessor;

        }

        private string GetUserId()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        }

        public async Task<PublicResponse> CreateLeave(CreateLeaveDto dto)
        {
            var response = new PublicResponse(true);

            try
            {
                var userId = GetUserId();

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return response.SetError("NOT_FOUND", "Employee not found");

                if (dto.StartDate < DateTime.Today)
                    return response.SetError("VALIDATION", "Nu poți selecta trecut");

                if (dto.EndDate < dto.StartDate)
                    return response.SetError("VALIDATION", "Interval invalid");

                var overlap = await _context.EmployeeLeaves
                    .AnyAsync(l =>
                        l.EmployeeId == employee.Id &&
                        l.Status != "Rejected" &&
                        dto.StartDate <= l.EndDate &&
                        dto.EndDate >= l.StartDate);

                if (overlap)
                    return response.SetError("VALIDATION", "Ai deja concediu în acest interval");

                var leave = new EmployeeLeave
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employee.Id,
                    LeaveType = dto.LeaveType,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Status = "Pending"
                };

                _context.EmployeeLeaves.Add(leave);
                await _context.SaveChangesAsync();

                return response.SetSuccess("Cerere trimisă");
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }

        public async Task<PublicResponse> GetMyLeaves()
        {
            var response = new PublicResponse(true);

            try
            {
                var userId = GetUserId();

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return response.SetError("NOT_FOUND", "Employee not found");

                var leaves = await _context.EmployeeLeaves
                    .Where(l => l.EmployeeId == employee.Id)
                    .Select(l => new
                    {
                        l.Id,
                        l.LeaveType,
                        l.StartDate,
                        l.EndDate,
                        l.Status
                    })
                    .ToListAsync();

                var usedDays = leaves
                    .Where(l => l.Status == "Approved" && l.LeaveType == "Vacation")
                    .Sum(l => (l.EndDate - l.StartDate).Days + 1);

                var totalDays = 21;

                return response.SetSuccess(new
                {
                    leaves,
                    totalDays,
                    usedDays,
                    remainingDays = totalDays - usedDays
                });
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }

        public async Task<PublicResponse> Approve(Guid id)
        {
            var response = new PublicResponse(true);

            try
            {
                var userId = GetUserId();

                var leave = await _context.EmployeeLeaves.FindAsync(id);

                if (leave == null)
                    return response.SetError("NOT_FOUND", "Concediul nu a fost găsit");

                if (leave.Status == "Approved")
                    return response.SetError("VALIDATION", "Deja aprobat");

                leave.Status = "Approved";
                leave.ApprovedBy = userId;

                await _context.SaveChangesAsync();

                return response.SetSuccess("Concediu aprobat");
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }

        public async Task<PublicResponse> Reject(Guid id)
        {
            var response = new PublicResponse(true);

            try
            {
                var userId = GetUserId();

                var leave = await _context.EmployeeLeaves.FindAsync(id);

                if (leave == null)
                    return response.SetError("NOT_FOUND", "Concediul nu a fost găsit");

                if (leave.Status == "Rejected")
                    return response.SetError("VALIDATION", "Deja respins");

                leave.Status = "Rejected";
                leave.ApprovedBy = userId;

                await _context.SaveChangesAsync();

                return response.SetSuccess("Concediu respins");
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }
    }
}

