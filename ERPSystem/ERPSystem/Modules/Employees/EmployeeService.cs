using ClosedXML.Excel;
using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Employees.Models;
using ERPSystem.Modules.Leaves;
using ERPSystem.Shared.BusinessLogic;
using ERPSystem.Shared.Notifications;
using ERPSystem.Utils.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace ERPSystem.Modules.Employees;

public class EmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly HolidayService _holidayService;
    private readonly NotificationsService _notificationService;
    private readonly ExcelExportService _excelExportService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmployeeService(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    HolidayService holidayService,
    NotificationsService notificationService,
    ExcelExportService excelExportService,
    IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _holidayService = holidayService;
        _notificationService = notificationService;
        _excelExportService = excelExportService;
        _httpContextAccessor = httpContextAccessor;
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
                    return response.SetError("VALIDATION", "Funcția este obligatorie.");

                if (request.HireDate == default)
                    return response.SetError("VALIDATION", "Data angajării este obligatorie");

                string? userId = null;
                string? firstName = request.FirstName;
                string? lastName = request.LastName;
                string? email = request.Email;

                if (request.Files != null)
                {
                    foreach (var file in request.Files)
                    {
                        Console.WriteLine(file.FileName);
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.UserId))
                {
                    var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.UserId);

                    if (user == null)
                        return response.SetError("NOT_FOUND", "Utilizatorul nu a fost găsit.");

                    userId = user.Id;
                    firstName = user.FirstName;
                    lastName = user.LastName;
                    email = user.Email;
                }

                if (string.IsNullOrWhiteSpace(firstName))
                    return response.SetError("VALIDATION", "Prenumele este obligatoriu.");

                if (string.IsNullOrWhiteSpace(lastName))
                    return response.SetError("VALIDATION", "Numele este obligatoriu.");

                var employee = new Employee
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

                if (request.Files.Any())
                {
                    var allowedExtensions = new[] {  ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".txt", ".ppt", ".pptx"  };

                    var root = Directory.GetCurrentDirectory();
                    var uploadPath = Path.Combine(root, "uploads", "employees", employee.Id.ToString());

                    Directory.CreateDirectory(uploadPath);

                    for (int i = 0; i < request.Files.Length; i++)
                    {
                        var file = request.Files[i];

                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                        if (!allowedExtensions.Contains(ext))
                            return response.SetError("VALIDATION", $"Tipul fișierului este invalid.: {file.FileName}");

                        if (file.Length > 1 * 1024 * 1024)
                            return response.SetError("VALIDATION", $"Fișierul este prea mare.: {file.FileName}");

                        var documentType =
                            request.DocumentTypes.Length > i
                                ? request.DocumentTypes[i]
                                : "Document";

                        if (string.IsNullOrWhiteSpace(documentType))
                            documentType = "Document";

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
                            ContentType = file.ContentType ?? "application/octet-stream",
                            DocumentType = documentType,
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

                if (string.IsNullOrWhiteSpace(employee.UserId))
                {
                    // 🔴 nu are cont → notificare Admin
                    await _notificationService.CreateNotificationForRolesAsync(
                        roleNames: new[] { "Admin" },
                        eventType: NotificationEvents.Employee,
                        title: "Angajat fără cont",
                        message: $"Angajatul {employee.FirstName} {employee.LastName} nu are cont. Creează un cont pentru acces în sistem.",
                        type: "Warning",
                        link: $"/admin/users",
                        entityType: "Employee",
                        entityId: employee.Id.ToString()
                    );
                }
                else
                {
                    // 🟢 are cont → notificare HR/Admin
                    await _notificationService.CreateNotificationForRolesAsync(
                        roleNames: new[] { "HR", "Admin", "Secretary" },
                        eventType: NotificationEvents.Employee,
                        title: "Angajat nou creat",
                        message: $"Angajatul {employee.FirstName} {employee.LastName} a fost creat în sistem.",
                        type: "Success",
                        link: $"/employee/{employee.Id}",
                        entityType: "Employee",
                        entityId: employee.Id.ToString()
                    );  
                }

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
                    return response.SetError("NOT_FOUND", "Angajatul nu a fost găsit.");
                }

           
                if (!string.IsNullOrWhiteSpace(request.UserId))
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(x => x.Id == request.UserId);

                    if (user == null)
                    {
                        return response.SetError("NOT_FOUND", "Utilizatorul nu a fost găsit.");
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

                await _notificationService.CreateNotificationForRolesAsync(
                     roleNames: new[] { "HR", "Admin", "Manager" },
                     eventType: NotificationEvents.Employee,
                     title: "Date angajat actualizate",
                     message: $"Datele angajatului {employee.FirstName} {employee.LastName} au fost actualizate.",
                     type: "Info",
                     link: $"/employee/{employee.Id}",
                     entityType: "Employee",
                     entityId: employee.Id.ToString()
                 );

                if (!string.IsNullOrWhiteSpace(employee.UserId))
                {
                    await _notificationService.CreateNotificationAsync(
                        userId: employee.UserId,
                        eventType: NotificationEvents.Employee,
                        title: "Profil actualizat",
                        message: "Datele profilului tău de angajat au fost actualizate.",
                        type: "Info",
                        link: "/profil-user",
                        entityType: "Employee",
                        entityId: employee.Id.ToString()
                    );
                }

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

    public async Task<PublicResponse> UploadEmployeeDocuments(HttpRequest httpRequest)
    {
        var response = new PublicResponse(true);

        var form = await httpRequest.ReadFormAsync();

        var employeeIdRaw = form["EmployeeId"].FirstOrDefault();

        if (!Guid.TryParse(employeeIdRaw, out var employeeId))
            return response.SetError("VALIDATION", "ID-ul angajatului este invalid.");

        var file = form.Files.GetFile("File");
        var documentType = form["DocumentType"].FirstOrDefault();

        var employee = await _context.Employees
            .FirstOrDefaultAsync(x => x.Id == employeeId);

        if (employee == null)
            return response.SetError("NOT_FOUND", "Angajatul nu a fost găsit.");

        if (file == null)
            return response.SetError("VALIDATION", "Nu a fost încărcat niciun fișier.");

        var allowedExtensions = new[]
        {
        ".pdf", ".jpg", ".jpeg", ".png",
        ".doc", ".docx", ".txt", ".ppt", ".pptx"
    };

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(ext))
            return response.SetError("VALIDATION", "Tipul fișierului este invalid");

        if (file.Length > 1 * 1024 * 1024)
            return response.SetError("VALIDATION", "Fișierul este prea mare.");

        var root = Directory.GetCurrentDirectory();
        var uploadPath = Path.Combine(root, "uploads", "employees", employee.Id.ToString());

        Directory.CreateDirectory(uploadPath);

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
            DocumentType = string.IsNullOrWhiteSpace(documentType) ? "Document" : documentType,
            UploadedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        await AddEmployeeActivityLogAsync(
            employee.Id,
            "EmployeeDocumentUploaded",
            $"A fost încărcat documentul {file.FileName} pentru angajatul {employee.FirstName} {employee.LastName}."
            
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

            var result = new PagedResult<EmployeeDto>
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
                 return response.SetError("NOT_FOUND", "Angajatul nu a fost găsit.");
         
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
                return response.SetError("NOT_FOUND", "Angajatul nu a fost găsit.");

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

            var pendingLeaves = await _context.EmployeeLeaves
                .Where(x =>
                    x.EmployeeId == employee.Id &&
                    x.Status == "Pending")
                .ToListAsync();

            foreach (var leave in pendingLeaves)
            {
                leave.Status = "Cancelled";
                leave.ReasonUpdate = "Cerere anulată automat deoarece angajatul a fost încetat.";
            }

            await _context.SaveChangesAsync();

            await AddEmployeeActivityLogAsync(
                 employee.Id,
                 "EmployeeTerminated",
                 $"Angajatul {employee.FirstName} {employee.LastName} a fost încetat la data {request.TerminationDate:dd.MM.yyyy}. Cereri pending anulate: {pendingLeaves.Count}."
             );

            await _notificationService.CreateNotificationForRolesAsync(
                roleNames: new[] { "HR", "Admin", "Secretary" },
                eventType: NotificationEvents.Employee,
                title: "Angajat încetat",
                message: $"Angajatul {employee.FirstName} {employee.LastName} a fost încetat la data {request.TerminationDate:dd.MM.yyyy}.",
                type: "Warning",
                link: $"/hr/employees/{employee.Id}",
                entityType: "Employee",
                entityId: employee.Id.ToString()
            );

            return response.SetSuccess("Employee terminated");
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
            PerformedBy = GetCurrentUser()
        });

        await _context.SaveChangesAsync();
    }

    public async Task<IResult> ViewEmployeeDocumentAsync(Guid documentId)
    {
        var document = await _context.EmployeeDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId);

        if (document == null)
            return Results.NotFound("Documentul nu a fost găsit.");

        if (!File.Exists(document.FilePath))
            return Results.NotFound("Fișierul nu a fost găsit.");

        var bytes = await File.ReadAllBytesAsync(document.FilePath);

        return Results.File(
            bytes,
            document.ContentType ?? "application/octet-stream",
            document.FileName
        );
    }

    public async Task<IResult> DownloadEmployeeDocumentAsync(Guid documentId)
    {
        var document = await _context.EmployeeDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId);

        if (document == null)
            return Results.NotFound("Documentul nu a fost găsit.");

        if (!File.Exists(document.FilePath))
            return Results.NotFound("Fișierul nu a fost găsit.");

        var bytes = await File.ReadAllBytesAsync(document.FilePath);

        return Results.File(
            bytes,
            document.ContentType ?? "application/octet-stream",
            document.FileName,
            enableRangeProcessing: true 
        );
    }

    public async Task<PublicResponse> DeleteEmployeeDocumentAsync(Guid documentId)
    {
        var response = new PublicResponse(true);

        try
        {
            var document = await _context.EmployeeDocuments
                .FirstOrDefaultAsync(x => x.Id == documentId);

            if (document == null)
                return response.SetError("NOT_FOUND", "Documentul nu a fost găsit.");

            if (File.Exists(document.FilePath))
            {
                File.Delete(document.FilePath);
            }

            _context.EmployeeDocuments.Remove(document);

            await _context.SaveChangesAsync();

            await AddEmployeeActivityLogAsync(
                document.EmployeeId,
                "EmployeeDocumentDeleted",
                $"Documentul '{document.FileName}' a fost șters."
            );

            return response.SetSuccess(true);
        }
        catch (Exception ex)
        {
            return response.SetError("SERVER", ex.Message);
        }
    }
    public async Task<IResult> ExportEmployeesExcelAsync( string? q,string? status, string? contractType)
    {
        var query = _context.Employees
            .Include(x => x.Address)
            .Include(x => x.Bank)
            .Include(x => x.Contact)
            .Include(x => x.Documents)
            .Include(x => x.Leaves)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.FirstName.Contains(q) ||
                x.LastName.Contains(q) ||
                (x.Email != null && x.Email.Contains(q)) ||
                x.JobTitle.Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.EmploymentStatus == status);

        if (!string.IsNullOrWhiteSpace(contractType))
            query = query.Where(x => x.ContractType == contractType);

        var employees = await query
            .OrderBy(x => x.EmploymentStatus == "Terminated")
            .ThenBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync();

        using var wb = new XLWorkbook();

        var employeesWs = wb.Worksheets.Add("Angajați");
        var detailsWs = wb.Worksheets.Add("Detalii");
        var documentsWs = wb.Worksheets.Add("Documente");
        var leavesWs = wb.Worksheets.Add("Concedii");

        var employeeHeaders = new[]
        {
        "ID", "Nume", "Prenume", "Email", "Funcție",
        "Status", "Tip contract", "Salariu",
        "Data angajării", "Data încetării",
        "Zile concediu/an", "Zile reportate",
        "Nr. documente", "Nr. concedii",
        "Creat la", "Actualizat la"
    };

        for (int i = 0; i < employeeHeaders.Length; i++)
            employeesWs.Cell(1, i + 1).Value = employeeHeaders[i];

        var row = 2;

        foreach (var e in employees)
        {
            employeesWs.Cell(row, 1).Value = e.Id.ToString();
            employeesWs.Cell(row, 2).Value = e.LastName;
            employeesWs.Cell(row, 3).Value = e.FirstName;
            employeesWs.Cell(row, 4).Value = e.Email ?? "";
            employeesWs.Cell(row, 5).Value = e.JobTitle;
            employeesWs.Cell(row, 6).Value = GetEmployeeStatusRo(e.EmploymentStatus);
            employeesWs.Cell(row, 7).Value = GetContractTypeRo(e.ContractType);
            employeesWs.Cell(row, 8).Value = e.Salary;
            employeesWs.Cell(row, 9).Value = e.HireDate;
            employeesWs.Cell(row, 10).Value = e.TerminationDate;
            employeesWs.Cell(row, 11).Value = e.VacationDaysPerYear;
            employeesWs.Cell(row, 12).Value = e.CarryOverDays;
            employeesWs.Cell(row, 13).Value = e.Documents?.Count ?? 0;
            employeesWs.Cell(row, 14).Value = e.Leaves?.Count ?? 0;
            employeesWs.Cell(row, 15).Value = e.CreatedAt;
            employeesWs.Cell(row, 16).Value = e.UpdatedAt;

            row++;
        }

        var detailsHeaders = new[]
        {
        "ID angajat", "Nume complet",
        "Telefon", "Contact urgență", "Telefon urgență",
        "Stradă", "Oraș", "Țară", "Cod poștal",
        "IBAN", "Bancă", "Observații"
    };

        for (int i = 0; i < detailsHeaders.Length; i++)
            detailsWs.Cell(1, i + 1).Value = detailsHeaders[i];

        row = 2;

        foreach (var e in employees)
        {
            employeesWs.Cell(row, 1).Value = e.Id.ToString();
            detailsWs.Cell(row, 2).Value = $"{e.LastName} {e.FirstName}";
            detailsWs.Cell(row, 3).Value = e.Contact?.PhoneNumber ?? "";
            detailsWs.Cell(row, 4).Value = e.Contact?.EmergencyContactName ?? "";
            detailsWs.Cell(row, 5).Value = e.Contact?.EmergencyContactPhone ?? "";
            detailsWs.Cell(row, 6).Value = e.Address?.Street ?? "";
            detailsWs.Cell(row, 7).Value = e.Address?.City ?? "";
            detailsWs.Cell(row, 8).Value = e.Address?.Country ?? "";
            detailsWs.Cell(row, 9).Value = e.Address?.PostalCode ?? "";
            detailsWs.Cell(row, 10).Value = e.Bank?.IBAN ?? "";
            detailsWs.Cell(row, 11).Value = e.Bank?.BankName ?? "";
            detailsWs.Cell(row, 12).Value = e.Notes ?? "";

            row++;
        }

        var documentHeaders = new[]
        {
        "ID document", "ID angajat", "Nume angajat",
        "Nume fișier", "Tip document", "Content type",
        "Cale fișier", "Încărcat la", "Încărcat de"
    };

        for (int i = 0; i < documentHeaders.Length; i++)
            documentsWs.Cell(1, i + 1).Value = documentHeaders[i];

        row = 2;

        foreach (var e in employees)
        {
            foreach (var d in (e.Documents ?? new List<EmployeeDocument>())
                .OrderByDescending(x => x.UploadedAt))
            {
                documentsWs.Cell(row, 1).Value = d.Id.ToString();
                documentsWs.Cell(row, 2).Value = e.Id.ToString();
                documentsWs.Cell(row, 3).Value = $"{e.LastName} {e.FirstName}";
                documentsWs.Cell(row, 4).Value = d.FileName;
                documentsWs.Cell(row, 5).Value = d.DocumentType;
                documentsWs.Cell(row, 6).Value = d.ContentType;
                documentsWs.Cell(row, 7).Value = d.FilePath;
                documentsWs.Cell(row, 8).Value = d.UploadedAt;
                documentsWs.Cell(row, 9).Value = d.UploadedBy ?? "";

                row++;
            }
        }

        var leaveHeaders = new[]
        {
        "ID concediu", "ID angajat", "Nume angajat",
        "Tip concediu", "Data început", "Data sfârșit",
        "Nr. zile", "Status", "Aprobat de",
        "Motiv / Observații", "Creat la"
    };

        for (int i = 0; i < leaveHeaders.Length; i++)
            leavesWs.Cell(1, i + 1).Value = leaveHeaders[i];

        row = 2;

        foreach (var e in employees)
        {
            foreach (var l in (e.Leaves ?? new List<EmployeeLeave>())
                .OrderByDescending(x => x.StartDate))
            {
                leavesWs.Cell(row, 1).Value = l.Id.ToString();
                leavesWs.Cell(row, 2).Value = e.Id.ToString();
                leavesWs.Cell(row, 3).Value = $"{e.LastName} {e.FirstName}";
                leavesWs.Cell(row, 4).Value = GetLeaveTypeRo(l.LeaveType);
                leavesWs.Cell(row, 5).Value = l.StartDate;
                leavesWs.Cell(row, 6).Value = l.EndDate;
                leavesWs.Cell(row, 7).Value = (l.EndDate.Date - l.StartDate.Date).Days + 1;
                leavesWs.Cell(row, 8).Value = GetLeaveStatusRo(l.Status);
                leavesWs.Cell(row, 9).Value = l.ApprovedBy ?? "";
                leavesWs.Cell(row, 10).Value = l.ReasonUpdate ?? "";
                leavesWs.Cell(row, 11).Value = l.CreatedAt;

                row++;
            }
        }

        _excelExportService.FormatSheet(employeesWs);
        _excelExportService.FormatSheet(detailsWs);
        _excelExportService.FormatSheet(documentsWs);
        _excelExportService.FormatSheet(leavesWs);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        return Results.File(
            ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"angajati_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx"
        );
    }

    private static string GetEmployeeStatusRo(string? status)
    {
        return status switch
        {
            "Active" => "Activ",
            "Terminated" => "Încetat",
            "Suspended" => "Suspendat",
            _ => status ?? ""
        };
    }

    private static string GetContractTypeRo(string? type)
    {
        return type switch
        {
            "FullTime" => "Normă întreagă",
            "PartTime" => "Part-time",
            "Collaboration" => "Colaborare",
            _ => type ?? ""
        };
    }

    private static string GetLeaveTypeRo(string? type)
    {
        return type switch
        {
            "Vacation" => "Concediu odihnă",
            "Medical" => "Medical",
            "Unpaid" => "Fără plată",
            _ => type ?? ""
        };
    }

    private static string GetLeaveStatusRo(string? status)
    {
        return status switch
        {
            "Pending" => "În așteptare",
            "Approved" => "Aprobat",
            "Rejected" => "Respins",
            _ => status ?? ""
        };
    }

    private string GetCurrentUser()
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? _httpContextAccessor.HttpContext?.User?
                .FindFirst("email")?.Value
            ?? _httpContextAccessor.HttpContext?.User?
                .FindFirst("username")?.Value
            ?? "system";
    }
}