using ClosedXML.Excel;
using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Models.Notifications;
using ERPSystem.Modules.Leaves.Models;
using ERPSystem.Shared.Notifications;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;


namespace ERPSystem.Modules.Leaves
{
    public class LeavesService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HolidayService _holidayService;
        private readonly NotificationsService _notificationService;



        public LeavesService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, HolidayService holidayService, NotificationsService notificationService)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _holidayService = holidayService;
            _notificationService = notificationService;
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

                if (employee.EmploymentStatus == "Terminated")
                    return response.SetError("FORBIDDEN", "Nu poți crea concediu pentru un angajat inactiv.");

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
                    return response.SetError("VALIDATION", "Concediu suprapus");

                var holidays = await _holidayService.GetHolidays(dto.StartDate.Year);

                var requestedDays = GetWorkingDays(
                    dto.StartDate,
                    dto.EndDate,
                    holidays
                );

                if (dto.LeaveType == "Vacation")
                {
                    var currentYear = DateTime.UtcNow.Year;

                    var approvedLeaves = await _context.EmployeeLeaves
                        .Where(l =>
                            l.EmployeeId == employee.Id &&
                            l.Status == "Approved" &&
                            l.LeaveType == "Vacation" &&
                            l.StartDate.Year == currentYear)
                        .ToListAsync();

                    var holidaysForRequest = await _holidayService.GetHolidays(dto.StartDate.Year);

                    var usedDays = approvedLeaves
                        .Sum(l => GetWorkingDays(l.StartDate, l.EndDate, holidaysForRequest));

                    var total = employee.VacationDaysPerYear + employee.CarryOverDays;

                    if (usedDays + requestedDays > total)
                        return response.SetError("VALIDATION", "Nu ai suficiente zile");
                }

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

                await _notificationService.CreateNotificationForRolesAsync(
                    roleNames: new[] { "HR", "Admin", "Secretary" },
                    eventType: NotificationEvents.Leave,
                    title: "Cerere nouă de concediu",
                    message: $"{employee.FirstName} {employee.LastName} a trimis o cerere de concediu pentru perioada {leave.StartDate:dd.MM.yyyy} - {leave.EndDate:dd.MM.yyyy}.",
                    type: "Info",
                    link: "/all-leaves",
                    entityType: "EmployeeLeave",
                    entityId: leave.Id.ToString()
                );

                await AddEmployeeLeaveActivityLogAsync(
                    leave.EmployeeId,
                    "LeaveCreated",
                    $"Cererea de concediu a fost creată pentru perioada {leave.StartDate:dd.MM.yyyy} - {leave.EndDate:dd.MM.yyyy}."
                );

                return response.SetSuccess("Cerere trimisă");
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }

        public async Task<PublicResponse> UpdateLeave(Guid id, CreateLeaveDto dto)
        {
            var response = new PublicResponse(true);

            try
            {
                var userId = GetUserId();

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return response.SetError("NOT_FOUND", "Employee not found");

                if (employee.EmploymentStatus == "Terminated")
                    return response.SetError("FORBIDDEN", "Nu poți modifica concediul.");

                var leave = await _context.EmployeeLeaves
                    .FirstOrDefaultAsync(l => l.Id == id && l.EmployeeId == employee.Id);

                if (leave == null)
                    return response.SetError("NOT_FOUND", "Cererea nu există");

                if (leave.Status != "Pending")
                    return response.SetError("VALIDATION", "Nu poți modifica această cerere");

                if (dto.StartDate < DateTime.Today)
                    return response.SetError("VALIDATION", "Nu poți selecta trecut");

                if (dto.EndDate < dto.StartDate)
                    return response.SetError("VALIDATION", "Interval invalid");

                var overlap = await _context.EmployeeLeaves
                    .AnyAsync(l =>
                        l.EmployeeId == employee.Id &&
                        l.Id != id &&
                        l.Status != "Rejected" &&
                        dto.StartDate <= l.EndDate &&
                        dto.EndDate >= l.StartDate);

                if (overlap)
                    return response.SetError("VALIDATION", "Concediu suprapus");

                var holidays = await _holidayService.GetHolidays(dto.StartDate.Year);

                var requestedDays = GetWorkingDays(
                    dto.StartDate,
                    dto.EndDate,
                    holidays
                );

                if (dto.LeaveType == "Vacation")
                {
                    var currentYear = DateTime.UtcNow.Year;

                    var approvedLeaves = await _context.EmployeeLeaves
                        .Where(l =>
                            l.EmployeeId == employee.Id &&
                            l.Id != id &&
                            l.Status == "Approved" &&
                            l.LeaveType == "Vacation" &&
                            l.StartDate.Year == currentYear)
                        .ToListAsync();

                    var holidaysForRequest = await _holidayService.GetHolidays(dto.StartDate.Year);

                    var usedDays = approvedLeaves
                        .Sum(l => GetWorkingDays(l.StartDate, l.EndDate, holidaysForRequest));

                    var total = employee.VacationDaysPerYear + employee.CarryOverDays;

                    if (usedDays + requestedDays > total)
                        return response.SetError("VALIDATION", "Nu ai suficiente zile");
                }

                leave.StartDate = dto.StartDate;
                leave.EndDate = dto.EndDate;
                leave.LeaveType = dto.LeaveType;
                leave.ReasonUpdate = dto.Reason;

                await _context.SaveChangesAsync();

                await _notificationService.CreateNotificationForRolesAsync(
                    roleNames: new[] { "HR", "Admin", "Secretary" },
                    eventType: NotificationEvents.Leave,
                    title: "Cerere de concediu actualizată",
                    message: $"{employee.FirstName} {employee.LastName} a actualizat cererea de concediu pentru perioada {leave.StartDate:dd.MM.yyyy} - {leave.EndDate:dd.MM.yyyy}.",
                    type: "Warning",
                    link: "/all-leaves",
                    entityType: "EmployeeLeave",
                    entityId: leave.Id.ToString()
                );

                await AddEmployeeLeaveActivityLogAsync(
                    leave.EmployeeId,
                    "LeaveUpdated",
                    $"Cererea de concediu a fost actualizată pentru perioada {leave.StartDate:dd.MM.yyyy} - {leave.EndDate:dd.MM.yyyy}."
                );

                return response.SetSuccess("Cerere actualizată");
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }

        public async Task<PublicResponse> CancelLeave(Guid id)
        {
            var response = new PublicResponse(true);

            try
            {
                var userId = GetUserId();

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return response.SetError("NOT_FOUND", "Employee not found");

                if (employee.EmploymentStatus == "Terminated")
                    return response.SetError("FORBIDDEN", "Nu poți anula concediul.");

                var leave = await _context.EmployeeLeaves
                    .FirstOrDefaultAsync(l => l.Id == id && l.EmployeeId == employee.Id);

                if (leave == null)
                    return response.SetError("NOT_FOUND", "Cererea nu există");

                if (leave.Status == "Rejected" || leave.Status == "Cancelled")
                    return response.SetError("VALIDATION", "Nu poți anula această cerere");

                if (leave.Status == "Approved" && leave.StartDate <= DateTime.Today)
                    return response.SetError("VALIDATION", "Nu poți anula un concediu deja început");

                leave.Status = "Cancelled";

                await _context.SaveChangesAsync();

                await _notificationService.CreateNotificationForRolesAsync(
                     roleNames: new[] { "HR", "Admin", "Secretary" },
                     eventType: NotificationEvents.Leave,
                     title: "Cerere de concediu anulată",
                     message: $"{employee.FirstName} {employee.LastName} a anulat cererea de concediu pentru perioada {leave.StartDate:dd.MM.yyyy} - {leave.EndDate:dd.MM.yyyy}.",
                     type: "Warning",
                     link: "/all-leaves",
                     entityType: "EmployeeLeave",
                     entityId: leave.Id.ToString()
                 );

                await AddEmployeeLeaveActivityLogAsync(
                    leave.EmployeeId,
                    "LeaveCancelled",
                    $"Cererea de concediu a fost anulată."
                );

                return response.SetSuccess("Cerere anulată");
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

                var currentYear = DateTime.UtcNow.Year;

                var leaves = await _context.EmployeeLeaves
                    .Where(l => l.EmployeeId == employee.Id)
                    .OrderByDescending(l => l.StartDate)
                    .ToListAsync();

                var vacationLeaves = leaves
                    .Where(l =>
                        l.Status == "Approved" &&
                        l.LeaveType == "Vacation" &&
                        l.StartDate.Year == currentYear);

                var holidays = await _holidayService.GetHolidays(currentYear);

                var usedVacationDays = vacationLeaves
                    .Sum(l => GetWorkingDays(l.StartDate, l.EndDate, holidays));

                var carryOver = employee.CarryOverDays;

                var total = employee.VacationDaysPerYear + carryOver;

                var remaining = total - usedVacationDays;

                var usedMedicalDays = leaves
                    .Where(l => l.Status == "Approved" && l.LeaveType == "Medical")
                    .Sum(l => GetWorkingDays(l.StartDate, l.EndDate, holidays));

                return response.SetSuccess(new
                {
                    leaves = leaves.Select(l => new
                    {
                        l.Id,
                        l.LeaveType,
                        l.StartDate,
                        l.EndDate,
                        l.Status,
                        Days = GetWorkingDays(l.StartDate, l.EndDate, holidays)
                    }),

                    vacation = new
                    {
                        total,
                        used = usedVacationDays,
                        remaining,
                        carryOver
                    },

                    medical = new
                    {
                        used = usedMedicalDays
                    }
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

                var leave = await _context.EmployeeLeaves
                    .Include(l => l.Employee)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (leave == null)
                    return response.SetError("NOT_FOUND", "Concediul nu a fost găsit");

                if (leave.Status != "Pending")
                    return response.SetError("VALIDATION", "Cererea nu mai poate fi aprobată");

                if (leave.LeaveType == "Vacation")
                {
                    var currentYear = DateTime.UtcNow.Year;

                    var holidays = await _holidayService.GetHolidays(currentYear);

                    var usedDays = await _context.EmployeeLeaves
                        .Where(l =>
                            l.EmployeeId == leave.EmployeeId &&
                            l.Status == "Approved" &&
                            l.LeaveType == "Vacation" &&
                            l.StartDate.Year == currentYear)
                        .ToListAsync();

                    var totalUsed = usedDays
                        .Sum(l => GetWorkingDays(l.StartDate, l.EndDate, holidays));

                    var requested = GetWorkingDays(leave.StartDate, leave.EndDate, holidays);

                    var total = leave.Employee.VacationDaysPerYear + leave.Employee.CarryOverDays;

                    if (totalUsed + requested > total)
                        return response.SetError("VALIDATION", "Nu sunt suficiente zile");
                }

                leave.Status = "Approved";
                leave.ApprovedBy = userId;

                await _context.SaveChangesAsync();

                await _notificationService.CreateNotificationAsync(
                    userId: leave.Employee.UserId,
                    eventType: NotificationEvents.Leave,
                    title: "Cerere de concediu aprobată",
                    message: $"Cererea ta de concediu din perioada {leave.StartDate:dd.MM.yyyy} - {leave.EndDate:dd.MM.yyyy} a fost aprobată.",
                    type: "Success",
                    link: "/profil-user",
                    entityType: "EmployeeLeave",
                    entityId: leave.Id.ToString()
                );

                await AddEmployeeLeaveActivityLogAsync(
                    leave.EmployeeId,
                    "LeaveApproved",
                    $"Cererea de concediu a fost aprobată."
                );

                return response.SetSuccess("Concediu aprobat");
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }

        public async Task<PublicResponse> Reject(Guid id, string? reason = null)
        {
            var response = new PublicResponse(true);

            try
            {
                var userId = GetUserId();

                var leave = await _context.EmployeeLeaves
                   .Include(l => l.Employee)
                   .FirstOrDefaultAsync(l => l.Id == id);
                   
                if (leave == null)
                    return response.SetError("NOT_FOUND", "Concediul nu a fost găsit");

                if (leave.Status != "Pending")
                    return response.SetError("VALIDATION", "Cererea nu mai poate fi respinsă");

                leave.Status = "Rejected";
                leave.ApprovedBy = userId;
                leave.ReasonUpdate = reason; 

                await _context.SaveChangesAsync();

                await _notificationService.CreateNotificationAsync(
                    userId: leave.Employee.UserId,
                    eventType: NotificationEvents.Leave,
                    title: "Cerere de concediu respinsă",
                    message: string.IsNullOrWhiteSpace(reason)
                        ? $"Cererea ta de concediu din perioada {leave.StartDate:dd.MM.yyyy} - {leave.EndDate:dd.MM.yyyy} a fost respinsă."
                        : $"Cererea ta de concediu din perioada {leave.StartDate:dd.MM.yyyy} - {leave.EndDate:dd.MM.yyyy} a fost respinsă. Motiv: {reason}",
                    type: "Warning",
                    link: "/profil-user",
                    entityType: "EmployeeLeave",
                    entityId: leave.Id.ToString()
                );

                await AddEmployeeLeaveActivityLogAsync(
                    leave.EmployeeId,
                    "LeaveRejected",
                    string.IsNullOrWhiteSpace(reason)
                        ? "Cererea de concediu a fost respinsă."
                        : $"Cererea de concediu a fost respinsă. Motiv: {reason}"
                );

                return response.SetSuccess("Concediu respins");
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }

        public async Task<PublicResponse> GetAllLeaves(GetLeavesQuery query)
        {
            var response = new PublicResponse(true);

            try
            {
                var currentYear = DateTime.UtcNow.Year;
                var holidays = await _holidayService.GetHolidays(currentYear);

                var q = _context.EmployeeLeaves
                    .Include(l => l.Employee)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(query.Status))
                    q = q.Where(l => l.Status == query.Status);

                if (!string.IsNullOrEmpty(query.LeaveType))
                    q = q.Where(l => l.LeaveType == query.LeaveType);

                if (query.EmployeeId.HasValue)
                    q = q.Where(l => l.EmployeeId == query.EmployeeId);

                if (query.StartFrom.HasValue)
                    q = q.Where(l => l.StartDate >= query.StartFrom);

                if (query.StartTo.HasValue)
                    q = q.Where(l => l.StartDate <= query.StartTo);

                q = query.SortBy switch
                {
                    "EndDate" => query.SortOrder == "asc"
                        ? q.OrderBy(l => l.EndDate)
                        : q.OrderByDescending(l => l.EndDate),

                    _ => query.SortOrder == "asc"
                        ? q.OrderBy(l => l.StartDate)
                        : q.OrderByDescending(l => l.StartDate)
                };

                var total = await q.CountAsync();

                var leaves = await q
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var result = leaves.Select(l => new
                {
                    l.Id,
                    l.LeaveType,
                    l.StartDate,
                    l.EndDate,
                    l.Status,
                    l.ReasonUpdate,

                    Employee = new
                    {
                        l.EmployeeId,
                        Name = l.Employee.FirstName + " " + l.Employee.LastName
                    },

                    Days = GetWorkingDays(l.StartDate, l.EndDate, holidays)
                });

                return response.SetSuccess(new
                {
                    data = result,
                    total,
                    page = query.Page,
                    pageSize = query.PageSize
                });
            }
            catch (Exception ex)
            {
                return response.SetError("SERVER", ex.Message);
            }
        }

        public async Task<PublicResponse> GetConflicts(DateTime start, DateTime end, Guid? excludeId)
        {
            if (start == default || end == default)
                return new PublicResponse(false)
                    .SetError("VALIDATION", "Perioada este invalidă");

            if (start > end)
                return new PublicResponse(false)
                    .SetError("VALIDATION", "Data de început nu poate fi mai mare decât data de sfârșit");

            var query = _context.EmployeeLeaves
                .AsNoTracking()
                .Include(l => l.Employee)
                .Where(l =>
                    l.Status == "Approved" &&
                    start.Date <= l.EndDate.Date &&
                    end.Date >= l.StartDate.Date);

            if (excludeId.HasValue)
                query = query.Where(l => l.Id != excludeId.Value);

            var conflicts = await query
                .OrderBy(l => l.StartDate)
                .Select(l => new 
                {
                    Id = l.Id,
                    EmployeeId = l.EmployeeId,
                    Employee = l.Employee != null
                        ? $"{l.Employee.FirstName} {l.Employee.LastName}"
                        : "Angajat necunoscut",
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    LeaveType = l.LeaveType,
                    Status = l.Status,
                    Days = (l.EndDate.Date - l.StartDate.Date).Days + 1
                })
                .ToListAsync();

            return new PublicResponse(true).SetSuccess(conflicts);
        }

        public async Task<byte[]> ExportLeaves()
        {
            var leaves = await _context.EmployeeLeaves
                .Include(l => l.Employee)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Leaves");

            ws.Cell(1, 1).Value = "Employee";
            ws.Cell(1, 2).Value = "Start";
            ws.Cell(1, 3).Value = "End";
            ws.Cell(1, 4).Value = "Type";
            ws.Cell(1, 5).Value = "Status";

            int row = 2;

            foreach (var l in leaves)
            {
                ws.Cell(row, 1).Value = l.Employee.FirstName + " " + l.Employee.LastName;
                ws.Cell(row, 2).Value = l.StartDate;
                ws.Cell(row, 3).Value = l.EndDate;
                ws.Cell(row, 4).Value = l.LeaveType;
                ws.Cell(row, 5).Value = l.Status;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }

        public static int GetWorkingDays(  DateTime start,  DateTime end, List<DateTime> holidays)
        {
            int days = 0;

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                var isWeekend =
                    date.DayOfWeek == DayOfWeek.Saturday ||
                    date.DayOfWeek == DayOfWeek.Sunday;

                var isHoliday = holidays.Contains(date.Date);

                if (!isWeekend && !isHoliday)
                    days++;
            }

            return days;
        }

        public async Task<List<string>> GetHolidays(int year)
        {
            var holidays = await _holidayService.GetHolidays(year);

            return holidays
                .Select(h => h.Date.ToString("yyyy-MM-dd"))
                .ToList();
        }

        private string GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value
                ?? GetUserId()
                ?? "system";
        }

        private async Task AddEmployeeLeaveActivityLogAsync( Guid employeeId, string action, string description)
        {
            _context.ActivityLog.Add(new ActivityLog
            {
                EntityType = "Employee",
                EntityId = employeeId.ToString(),
                Action = action,
                Description = description,
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = GetCurrentUser()
            });

            await _context.SaveChangesAsync();
        }




    }
}

