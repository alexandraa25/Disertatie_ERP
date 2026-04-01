using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Employees.Models;
using ERPSystem.Modules.Leaves;
using ERPSystem.Utils.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Modules.Employees;

public class EmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly HolidayService _holidayService;
   

    public EmployeeService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, HolidayService holidayService)
    {
        _context = context;
        _userManager = userManager;
        _holidayService = holidayService;
     
    }

    public async Task<PublicResponse> CreateEmployeeFullAsync(CreateEmployeeFullRequest request)
    {
        var response = new PublicResponse(true);

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrEmpty(request.JobTitle))
                    return response.SetError("VALIDATION", "Job title is required");

                if (request.HireDate == default)
                    return response.SetError("VALIDATION", "Hire date is required");

                var employee = new Employee
                {
                    Id = Guid.NewGuid(),
                    UserId = null, // 🔥 NU mai creezi user
                    FirstName = request.FirstName!,
                    LastName = request.LastName!,
                    Email = request.Email,
                    HireDate = request.HireDate,
                    JobTitle = request.JobTitle,
                    Salary = request.Salary,
                    ContractType = request.ContractType,
                    Notes = request.Notes,
                    EmploymentStatus = "Active"
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return response.SetSuccess(employee.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return response.SetError("SERVER", ex.Message);
            }
        });
    }

    public async Task<PublicResponse> UploadEmployeeDocuments(Guid employeeId, IFormFileCollection files)
    {
        var response = new PublicResponse(true);

        try
        {
            var employee = await _context.Employees.FindAsync(employeeId);

            if (employee == null)
                return response.SetError("NOT_FOUND", "Employee not found");

            if (files == null || files.Count == 0)
                return response.SetError("VALIDATION", "No files uploaded");

            var allowedExtensions = new[] { ".pdf", ".jpg", ".png" };

            var root = Directory.GetCurrentDirectory();
            var uploadPath = Path.Combine(root, "uploads", "employees", employeeId.ToString());

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var uploadedFiles = new List<object>();

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.FileName).ToLower();

                if (!allowedExtensions.Contains(ext))
                    return response.SetError("VALIDATION", $"Invalid file: {file.FileName}");

                if (file.Length > 5 * 1024 * 1024)
                    return response.SetError("VALIDATION", $"File too large: {file.FileName}");

                var newFileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadPath, newFileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                var doc = new EmployeeDocument
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employeeId,
                    FileName = file.FileName,
                    FilePath = filePath,
                    ContentType = file.ContentType,
                    UploadedAt = DateTime.UtcNow
                };

                _context.EmployeeDocuments.Add(doc);

                uploadedFiles.Add(new
                {
                    doc.Id,
                    doc.FileName
                });
            }

            await _context.SaveChangesAsync();

            return response.SetSuccess(uploadedFiles);
        }
        catch (Exception ex)
        {
            return response.SetError("SERVER", ex.Message);
        }
    }

    public async Task<PublicResponse> GetEmployeesAsync()
    {
        var response = new PublicResponse(true);

        try
        {
            var employees = await _context.Employees
                .Include(x => x.User)
                .Select(x => new EmployeeDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    FirstName = x.User != null ? x.User.FirstName : null,
                    LastName = x.User != null ? x.User.LastName : null,
                    JobTitle = x.JobTitle,
                    HireDate = x.HireDate,
                    TerminationDate = x.TerminationDate,
                    EmploymentStatus = x.EmploymentStatus,
                    Salary = x.Salary,
                    ContractType = x.ContractType
                })
                .ToListAsync();

            return response.SetSuccess(employees);
        }
        catch (Exception ex)
        {
            return response.SetError("SERVER", ex.Message);
        }
    }

    public async Task<PublicResponse> GetEmployeeAsync(Guid id)
    {
        var response = new PublicResponse(true);

        try
        {

            var currentYear = DateTime.UtcNow.Year;
            var holidays = await _holidayService.GetHolidays(currentYear);

            var employee = await _context.Employees
               .Include(x => x.User)
               .Include(x => x.Address)
               .Include(x => x.Bank)
               .Include(x => x.Contact)
               .Include(x => x.Documents)
               .Include(x => x.Leaves)
               .Where(x => x.Id == id)
               .Select(x => new
               {
                   x.Id,
                   x.FirstName,
                   x.LastName,
                   x.Email,
                   x.JobTitle,
                   x.HireDate,
                   x.EmploymentStatus,
                   x.Salary,
                   x.ContractType,
            
                   Address = x.Address,
                   Bank = x.Bank,
                   Contact = x.Contact,
                    Documents = x.Documents.Select(d => new
                    {
                        d.Id,
                        d.FileName,
                        d.FilePath,
                        d.ContentType,
                        d.UploadedAt
                    }).ToList(),
                   Leaves = x.Leaves
                         .OrderByDescending(l => l.StartDate)
                         .Select(l => new
                         {
                             l.Id,
                             l.LeaveType,
                             l.StartDate,
                             l.EndDate,
                             l.Status,
                             l.ReasonUpdate,

                             Days = LeavesService.GetWorkingDays(l.StartDate, l.EndDate, holidays)
                         }).ToList()
               })
               .FirstOrDefaultAsync();

         

            if (employee == null)
                return response.SetError("NOT_FOUND", "Employee not found");

            return response.SetSuccess(employee);
        }
        catch (Exception ex)
        {
            return response.SetError("SERVER", ex.Message);
        }
    }

    public async Task<PublicResponse> TerminateEmployeeAsync(Guid employeeId, TerminateEmployeeRequest request)
    {
        var response = new PublicResponse(true);

        try
        {
            var employee = await _context.Employees
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == employeeId);

            if (employee == null)
                return response.SetError("NOT_FOUND", "Employee not found");

            employee.TerminationDate = request.TerminationDate;
            employee.EmploymentStatus = "Terminated";

            if (employee.User != null)
                employee.User.IsActive = false;

            await _context.SaveChangesAsync();

            return response.SetSuccess("Employee terminated");
        }
        catch (Exception ex)
        {
            return response.SetError("SERVER", ex.Message);
        }
    }

    public async Task<PublicResponse> GetDashboardAsync()
    {
        var response = new PublicResponse(true);

        try
        {
            var total = await _context.Employees.CountAsync();

            var active = await _context.Employees
                .CountAsync(x => x.EmploymentStatus == "Active");

            var terminated = await _context.Employees
                .CountAsync(x => x.EmploymentStatus == "Terminated");

            var month = DateTime.UtcNow.Month;
            var year = DateTime.UtcNow.Year;

            var newHires = await _context.Employees
                .CountAsync(x => x.HireDate.Month == month && x.HireDate.Year == year);

            var dashboard = new HrDashboardDto
            {
                TotalEmployees = total,
                ActiveEmployees = active,
                TerminatedEmployees = terminated,
                NewHiresThisMonth = newHires
            };

            return response.SetSuccess(dashboard);
        }
        catch (Exception ex)
        {
            return response.SetError("SERVER", ex.Message);
        }
    }

    public async Task<PublicResponse> GetSimpleUsers()
    {
        var response = new PublicResponse(true);

        try
        {
            var users = await _userManager.Users.ToListAsync();

            var result = new List<object>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    Roles = roles
                });
            }

            return response.SetSuccess(result);
        }
        catch (Exception ex)
        {
            return response.SetError("SERVER", ex.Message);
        }
    }
}