using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Employees.Models;

using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Modules.Employees;

public class EmployeeService
{
    private readonly ApplicationDbContext _context;

    public EmployeeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IResult> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        if (request.UserId != null)
        {
            var existing = await _context.Employees
                .FirstOrDefaultAsync(x => x.UserId == request.UserId);

            if (existing != null)
                return Results.BadRequest("User already has an employee profile");
        }

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            HireDate = request.HireDate,
            JobTitle = request.JobTitle,
            EmploymentStatus = request.EmploymentStatus ?? "Active",
            Salary = request.Salary,
            ContractType = request.ContractType,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);

        await _context.SaveChangesAsync();

        return Results.Ok(employee);
    }

    public async Task<IResult> GetEmployeesAsync()
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

        return Results.Ok(employees);
    }

    public async Task<IResult> GetEmployeeAsync(Guid id)
    {
        var employee = await _context.Employees
            .Include(x => x.User)
            .Where(x => x.Id == id)
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
            .FirstOrDefaultAsync();

        if (employee == null)
            return Results.NotFound();

        return Results.Ok(employee);
    }

    public async Task<IResult> TerminateEmployeeAsync(Guid employeeId, TerminateEmployeeRequest request)
    {
        var employee = await _context.Employees
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == employeeId);

        if (employee == null)
            return Results.NotFound();

        employee.TerminationDate = request.TerminationDate;
        employee.EmploymentStatus = "Terminated";

        if (employee.User != null)
            employee.User.IsActive = false;

        await _context.SaveChangesAsync();

        return Results.Ok();
    }

    public async Task<IResult> GetDashboardAsync()
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

        return Results.Ok(dashboard);
    }
}