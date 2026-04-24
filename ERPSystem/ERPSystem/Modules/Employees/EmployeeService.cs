using DocumentFormat.OpenXml.Office2010.Excel;
using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Employees.Models;
using ERPSystem.Modules.Leaves;
using ERPSystem.Modules.Student.Models;
using ERPSystem.Utils.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static ERPSystem.Utils.Constants.General.Route;

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
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var response = new PublicResponse(true);

            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                if (string.IsNullOrWhiteSpace(request.JobTitle))
                    return response.SetError("VALIDATION", "Job title is required");

                if (request.HireDate == default)
                    return response.SetError("VALIDATION", "Hire date is required");

                string? userId = null;
                string? firstName = request.FirstName;
                string? lastName = request.LastName;
                string? email = request.Email;

                if (!string.IsNullOrWhiteSpace(request.UserId))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.UserId);

                    if (user == null)
                        return response.SetError("NOT_FOUND", "User not found");

                    userId = user.Id;
                    firstName = user.FirstName;
                    lastName = user.LastName;
                    email = user.Email;
                }

                if (string.IsNullOrWhiteSpace(firstName))
                    return response.SetError("VALIDATION", "First name is required");

                if (string.IsNullOrWhiteSpace(lastName))
                    return response.SetError("VALIDATION", "Last name is required");

                var employee = new Data.Entities.Employee
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    HireDate = request.HireDate,
                    JobTitle = request.JobTitle,
                    Salary = request.Salary,
                    ContractType = request.ContractType,
                    Notes = request.Notes,
                    EmploymentStatus = "Active"
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                _context.EmployeeContact.Add(new EmployeeContact
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employee.Id,
                    PhoneNumber = request.PhoneNumber,
                    EmergencyContactName = request.EmergencyContactName,
                    EmergencyContactPhone = request.EmergencyContactPhone
                });

                _context.EmployeeAddress.Add(new EmployeeAddress
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employee.Id,
                    Street = request.Street,
                    City = request.City,
                    Country = request.Country,
                    PostalCode = request.PostalCode
                });

                _context.EmployeeBank.Add(new EmployeeBank
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employee.Id,
                    IBAN = request.IBAN,
                    BankName = request.BankName
                });

                if (request.Files != null && request.Files.Any())
                {
                    var allowedExtensions = new[]
                    {
                    ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".txt", ".ppt", ".pptx"
                };

                    var root = Directory.GetCurrentDirectory();
                    var uploadPath = Path.Combine(root, "uploads", "employees", employee.Id.ToString());

                    Directory.CreateDirectory(uploadPath);

                    foreach (var file in request.Files)
                    {
                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(ext))
                            return response.SetError("VALIDATION", $"Invalid file: {file.FileName}");

                        if (file.Length > 1 * 1024 * 1024)
                            return response.SetError("VALIDATION", $"File too large: {file.FileName}");

                        var newFileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadPath, newFileName);

                        await using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        _context.EmployeeDocuments.Add(new EmployeeDocument
                        {
                            Id = Guid.NewGuid(),
                            EmployeeId = employee.Id,
                            FileName = file.FileName,
                            FilePath = filePath,
                            ContentType = file.ContentType,
                            UploadedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await AddEmployeeActivityLogAsync(
                    employee.Id,
                    "EmployeeCreated",
                    $"Angajatul {employee.FirstName} {employee.LastName} a fost creat."
                );

                return response.SetSuccess(employee.Id);
            }
            catch (Exception ex)
            {
                return new PublicResponse(false).SetError("SERVER", ex.ToString());
            }
        });
    }

    public async Task<PublicResponse> UpdateEmployeeAsync(UpdateEmployeeRequest request)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var response = new PublicResponse(true);

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (employee == null)
                {
                    return response.SetError("NOT_FOUND", "Employee not found");
                }

           
                if (!string.IsNullOrWhiteSpace(request.UserId))
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(x => x.Id == request.UserId);

                    if (user == null)
                    {
                        return response.SetError("NOT_FOUND", "User not found");
                    }

                    employee.UserId = user.Id;
                    employee.FirstName = user.FirstName;
                    employee.LastName = user.LastName;
                    employee.Email = user.Email;
                }
                else
                {
                    employee.FirstName = request.FirstName;
                    employee.LastName = request.LastName;
                    employee.Email = request.Email;
                }

                employee.HireDate = request.HireDate;
                employee.JobTitle = request.JobTitle;
                employee.Salary = request.Salary;
                employee.ContractType = request.ContractType;
                employee.Notes = request.Notes;
                employee.UpdatedAt = DateTime.UtcNow;

                var contact = await _context.EmployeeContact
                    .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id);

                if (contact == null)
                {
                    contact = new EmployeeContact
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = employee.Id
                    };

                    _context.EmployeeContact.Add(contact);
                }

                contact.PhoneNumber = request.PhoneNumber ?? contact.PhoneNumber;
                contact.EmergencyContactName = request.EmergencyContactName ?? contact.EmergencyContactName;
                contact.EmergencyContactPhone = request.EmergencyContactPhone ?? contact.EmergencyContactPhone;

                var address = await _context.EmployeeAddress
                    .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id);

                if (address == null)
                {
                    address = new EmployeeAddress
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = employee.Id,
                        Street = request.Street ?? "",
                        City = request.City ?? "",
                        Country = request.Country ?? "",
                        PostalCode = request.PostalCode ?? ""
                    };

                    _context.EmployeeAddress.Add(address);
                }
                else
                {
                    address.Street = request.Street ?? address.Street;
                    address.City = request.City ?? address.City;
                    address.Country = request.Country ?? address.Country;
                    address.PostalCode = request.PostalCode ?? address.PostalCode;
                }

                var bank = await _context.EmployeeBank
                    .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id);

                if (bank == null)
                {
                    bank = new EmployeeBank
                    {
                        Id = Guid.NewGuid(),
                        EmployeeId = employee.Id,
                        IBAN = request.IBAN ?? "",
                        BankName = request.BankName ?? ""
                    };

                    _context.EmployeeBank.Add(bank);
                }
                else
                {
                    bank.IBAN = request.IBAN ?? bank.IBAN;
                    bank.BankName = request.BankName ?? bank.BankName;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await AddEmployeeActivityLogAsync(
                    employee.Id,
                    "EmployeeUpdated",
                    $"Datele angajatului {employee.FirstName} {employee.LastName} au fost actualizate."
                );

                return response.SetSuccess(employee.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new PublicResponse(false)
                    .SetError("SERVER", ex.Message);
            }
        });
    }

    public async Task<PublicResponse> UploadEmployeeDocuments(UploadEmployeeDocsRequest request)
    {
        var response = new PublicResponse(true);

        var employee = await _context.Employees
            .FirstOrDefaultAsync(x => x.Id == request.EmployeeId);

        if (employee == null)
            return response.SetError("NOT_FOUND", "Employee not found");

        if (request.Files == null || !request.Files.Any())
            return response.SetError("VALIDATION", "No files uploaded");

        var root = Directory.GetCurrentDirectory();
        var uploadPath = Path.Combine(root, "uploads", "employees", employee.Id.ToString());

        Directory.CreateDirectory(uploadPath);

        for (int i = 0; i < request.Files.Count; i++)
        {
            var file = request.Files[i];
            var docType = request.DocumentTypes.ElementAtOrDefault(i) ?? "Unknown";

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var path = Path.Combine(uploadPath, fileName);

            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            _context.EmployeeDocuments.Add(new EmployeeDocument
            {
                Id = Guid.NewGuid(),
                EmployeeId = employee.Id,
                FileName = file.FileName,
                FilePath = path,
                ContentType = file.ContentType ?? "application/octet-stream",
                DocumentType = docType, // 🔥 aici
                UploadedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        await AddEmployeeActivityLogAsync(
            employee.Id,
            "EmployeeDocumentsUploaded",
            $"Au fost încărcate {request.Files.Count} document(e) pentru angajatul {employee.FirstName} {employee.LastName}."
        );

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> GetEmployeesAsync(EmployeeListRequest request)
    {
        var response = new PublicResponse(true);

        try
        {
            var query = _context.Employees
                .Include(x => x.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim().ToLower();

                query = query.Where(x =>
                    ((x.User != null ? x.User.FirstName : x.FirstName) ?? "").ToLower().Contains(search) ||
                    ((x.User != null ? x.User.LastName : x.LastName) ?? "").ToLower().Contains(search) ||
                    (x.JobTitle ?? "").ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(request.EmploymentStatus))
            {
                query = query.Where(x => x.EmploymentStatus == request.EmploymentStatus);
            }

            if (!string.IsNullOrWhiteSpace(request.ContractType))
            {
                query = query.Where(x => x.ContractType == request.ContractType);
            }

            if (!string.IsNullOrWhiteSpace(request.JobTitle))
            {
                query = query.Where(x => x.JobTitle == request.JobTitle);
            }

            if (request.HireDateFrom.HasValue)
            {
                query = query.Where(x => x.HireDate >= request.HireDateFrom.Value);
            }

            if (request.HireDateTo.HasValue)
            {
                query = query.Where(x => x.HireDate <= request.HireDateTo.Value);
            }

            var sortBy = request.SortBy?.ToLower();
            var sortDirection = request.SortDirection?.ToLower() == "asc" ? "asc" : "desc";

            query = (sortBy, sortDirection) switch
            {
                ("name", "asc") => query.OrderBy(x => x.User != null ? x.User.FirstName : x.FirstName)
                                        .ThenBy(x => x.User != null ? x.User.LastName : x.LastName),

                ("name", "desc") => query.OrderByDescending(x => x.User != null ? x.User.FirstName : x.FirstName)
                                         .ThenByDescending(x => x.User != null ? x.User.LastName : x.LastName),

                ("salary", "asc") => query.OrderBy(x => x.Salary),
                ("salary", "desc") => query.OrderByDescending(x => x.Salary),

                ("hiredate", "asc") => query.OrderBy(x => x.HireDate),
                ("hiredate", "desc") => query.OrderByDescending(x => x.HireDate),

                _ => query.OrderByDescending(x => x.HireDate)
            };

            var totalCount = await query.CountAsync();

            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            var employees = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EmployeeDto
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    FirstName = x.User != null ? x.User.FirstName : x.FirstName,
                    LastName = x.User != null ? x.User.LastName : x.LastName,
                    JobTitle = x.JobTitle,
                    HireDate = x.HireDate,
                    TerminationDate = x.TerminationDate,
                    EmploymentStatus = x.EmploymentStatus,
                    Salary = x.Salary,
                    ContractType = x.ContractType
                })
                .ToListAsync();

            var result = new Utils.Response.PagedResult<EmployeeDto>
            {
                Items = employees,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return response.SetSuccess(result);
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
                   x.UserId,
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
                       d.DocumentType,
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

            if (request.File != null)
            {
                var root = Directory.GetCurrentDirectory();
                var uploadPath = Path.Combine(root, "uploads", "employees", employee.Id.ToString());

                Directory.CreateDirectory(uploadPath);

                var ext = Path.GetExtension(request.File.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var path = Path.Combine(uploadPath, fileName);

                await using var stream = new FileStream(path, FileMode.Create);
                await request.File.CopyToAsync(stream);

                _context.EmployeeDocuments.Add(new EmployeeDocument
                {
                    Id = Guid.NewGuid(),
                    EmployeeId = employee.Id,
                    FileName = request.File.FileName,
                    FilePath = path,
                    ContentType = request.File.ContentType ?? "application/octet-stream",
                    DocumentType = request.DocumentType ?? "Incetare",
                    UploadedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            await AddEmployeeActivityLogAsync(
                employee.Id,
                "EmployeeTerminated",
                $"Angajatul {employee.FirstName} {employee.LastName} a fost încetat la data {request.TerminationDate:dd.MM.yyyy}."
            );

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
            var usedUserIds = await _context.Employees
                .Where(e => e.UserId != null)
                .Select(e => e.UserId)
                .ToListAsync();

            var users = await _userManager.Users
                .Where(u => !usedUserIds.Contains(u.Id))
                .ToListAsync();

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

    private async Task AddEmployeeActivityLogAsync(
    Guid employeeId,
    string action,
    string description)
    {
        _context.ActivityLog.Add(new ActivityLog
        {
            EntityType = "Employee",
            EntityId = employeeId.ToString(),
            Action = action,
            Description = description,
            CreatedAtUtc = DateTime.UtcNow,
            PerformedBy = "system"
        });

        await _context.SaveChangesAsync();
    }
}