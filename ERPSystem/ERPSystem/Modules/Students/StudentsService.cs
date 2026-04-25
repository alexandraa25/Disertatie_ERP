using ClosedXML.Excel;
using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Student.Models;
using ERPSystem.Modules.Students.Models;
using ERPSystem.Shared.Notifications;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;


namespace ERPSystem.Modules.Student;

public class StudentsService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<StudentsService> _logger;
    private readonly NotificationsService _notificationService;


    public StudentsService(ApplicationDbContext db, ILogger<StudentsService> logger, NotificationsService notificationservice)
    {
        _db = db;
        _logger = logger;
        _notificationService = notificationservice;
    }

    public async Task<PublicResponse> GetStudentsAsync(  string? q,  int page,   int pageSize,  string? sortBy,  string? sortDir,  int? recentDays,  bool? onlyRecent, int? sessionId)
    {
        var response = new PublicResponse(true);

        var query = BuildStudentsQuery(q, sortBy, sortDir, onlyRecent, recentDays, sessionId);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StudentListItemDto(
                s.Id,
                s.FullName,
                s.Email,
                s.Phone,
                s.IsActive,
                s.CreatedAtUtc
            ))
            .ToListAsync();

        return response.SetSuccess(new Student.Models.PagedResult<StudentListItemDto>(page, pageSize, total, items));
    }

    public async Task<PublicResponse> GetByIdAsync(int id)
    {
        var response = new PublicResponse(true);

        try
        {
            var s = await _db.Students
                .AsNoTracking()
                .Include(x => x.StudentGuardians)
                    .ThenInclude(sg => sg.Guardian)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (s is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Student not found");

            var guardians = s.StudentGuardians
                .Select(sg => new GuardianDto(
                    sg.Guardian.Id,
                    sg.Guardian.FirstName,
                    sg.Guardian.LastName,
                    sg.Guardian.Email,
                    sg.Guardian.Phone,
                    sg.RelationshipType,
                    sg.IsPrimaryContact
                ))
                .ToList();

            var dto = new StudentDetailsDto(
                s.Id,
                s.FullName,
                s.FirstName,
                s.LastName,
                s.Email,
                s.Phone,
                s.Address,
                s.DateOfBirth,
                s.IsActive,
                guardians
            );

            return response.SetSuccess(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByIdAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> CreateAsync(CreateStudentDto dto)
    {
        var response = new PublicResponse(true);

        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
                return response.SetError(ErrorCodes.InvalidParameters, "FullName is required");

            var s = new Data.Entities.Student
            {
                FullName = dto.FullName.Trim(),
                FirstName = string.IsNullOrWhiteSpace(dto.FirstName) ? null : dto.FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(dto.LastName) ? null : dto.LastName.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
                Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
                DateOfBirth = dto.DateOfBirth,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            if (s.IsMinor && (dto.Guardians == null || dto.Guardians.Count == 0))
                return response.SetError(ErrorCodes.InvalidParameters,
                    "Student minor trebuie să aibă cel puțin un părinte.");

            if (dto.Guardians != null && dto.Guardians.Count(g => g.IsPrimaryContact) > 1)
                return response.SetError(ErrorCodes.InvalidParameters,
                    "Poate exista un singur contact principal.");

            if (dto.Guardians != null)
            {
                foreach (var g in dto.Guardians)
                {
                    var email = g.Email.Trim();

                    var guardian = await _db.Guardians
                        .FirstOrDefaultAsync(x => x.Email == email);

                    if (guardian == null)
                    {
                        guardian = new Guardian
                        {
                            FirstName = g.FirstName.Trim(),
                            LastName = g.LastName.Trim(),
                            Email = email,
                            Phone = g.Phone.Trim(),
                            CreatedAtUtc = DateTime.UtcNow,
                            UpdatedAtUtc = DateTime.UtcNow
                        };

                        _db.Guardians.Add(guardian);
                    }

                    s.StudentGuardians.Add(new StudentGuardian
                    {
                        Guardian = guardian,
                        RelationshipType = g.RelationshipType,
                        IsPrimaryContact = g.IsPrimaryContact
                    });
                }
            }

            _db.Students.Add(s);
            await _db.SaveChangesAsync();

            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = "Student",
                EntityId = s.Id.ToString(),
                Action = "Create",
                Description = $"Cursantul {s.FullName} a fost creat.",
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = "system"
            });

            await _notificationService.CreateNotificationForRolesAsync(
                 roleNames: new[] { "Admin", "Secretary" },
                 eventType: NotificationEvents.StudentActivity,
                 title: "Date cursant actualizate",
                 message: $"Datele cursantului {s.FullName} au fost actualizate.",
                 type: "Info",
                 link: $"/students/{s.Id}",
                 entityType: "Student",
                 entityId: s.Id.ToString()
             );
            await transaction.CommitAsync();

            return response.SetCreated(new { id = s.Id });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "CreateAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> UpdateAsync(int id, UpdateStudentDto dto)
    {
        var response = new PublicResponse(true);

        try
        {
            var s = await _db.Students
                .Include(x => x.StudentGuardians)
                    .ThenInclude(sg => sg.Guardian)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (s is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Student not found");

            var oldGuardians = s.StudentGuardians
                .Select(x => x.Guardian.Email)
                .OrderBy(x => x)
                .ToList();

            s.FullName = dto.FullName.Trim();
            s.FirstName = string.IsNullOrWhiteSpace(dto.FirstName) ? null : dto.FirstName.Trim();
            s.LastName = string.IsNullOrWhiteSpace(dto.LastName) ? null : dto.LastName.Trim();
            s.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            s.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
            s.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
            s.DateOfBirth = dto.DateOfBirth;
            s.IsActive = dto.IsActive;
            s.UpdatedAtUtc = DateTime.UtcNow;

            if (s.IsMinor && (dto.Guardians == null || dto.Guardians.Count == 0))
                return response.SetError(ErrorCodes.InvalidParameters,
                    "Student minor trebuie să aibă cel puțin un părinte.");

            List<string> newGuardians = new();

            if (dto.Guardians != null)
            {
                if (dto.Guardians.Count(g => g.IsPrimaryContact) > 1)
                    return response.SetError(ErrorCodes.InvalidParameters,
                        "Poate exista un singur contact principal.");

                _db.StudentGuardians.RemoveRange(s.StudentGuardians);

                foreach (var g in dto.Guardians)
                {
                    var email = g.Email.Trim();
                    newGuardians.Add(email);

                    var guardian = await _db.Guardians
                        .FirstOrDefaultAsync(x => x.Email == email);

                    if (guardian == null)
                    {
                        guardian = new Guardian
                        {
                            FirstName = g.FirstName.Trim(),
                            LastName = g.LastName.Trim(),
                            Email = email,
                            Phone = g.Phone.Trim(),
                            CreatedAtUtc = DateTime.UtcNow,
                            UpdatedAtUtc = DateTime.UtcNow
                        };

                        _db.Guardians.Add(guardian);
                    }

                    s.StudentGuardians.Add(new StudentGuardian
                    {
                        Guardian = guardian,
                        RelationshipType = g.RelationshipType,
                        IsPrimaryContact = g.IsPrimaryContact
                    });
                }
            }

            newGuardians = newGuardians.OrderBy(x => x).ToList();
            var guardiansChanged = !oldGuardians.SequenceEqual(newGuardians);

            await _db.SaveChangesAsync();

            

            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = "Student",
                EntityId = s.Id.ToString(),
                Action = "Update",
                Description = $"Datele cursantului {s.FullName} au fost actualizate.",
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = "system"
            });

            await _db.SaveChangesAsync();

            await _notificationService.CreateNotificationForRolesAsync(
                 roleNames: new[] { "Admin", "Secretary" },
                 eventType: NotificationEvents.StudentActivity,
                 title: "Date cursant actualizate",
                 message: $"Datele cursantului {s.FullName} au fost actualizate.",
                 type: "Info",
                 link: $"/students/{s.Id}",
                 entityType: "Student",
                 entityId: s.Id.ToString()
             );

            if (guardiansChanged)
            {
                var added = newGuardians.Except(oldGuardians).ToList();
                var removed = oldGuardians.Except(newGuardians).ToList();

                var description = "Tutorii elevului au fost actualizați";

                if (added.Any() || removed.Any())
                {
                    description += ". ";

                    if (added.Any())
                        description += $"Adăugați: {string.Join(", ", added)}. ";

                    if (removed.Any())
                        description += $"Eliminați: {string.Join(", ", removed)}.";
                }

                _db.ActivityLog.Add(new ActivityLog
                {
                    EntityType = "Student",
                    EntityId = s.Id.ToString(),
                    Action = "UpdateGuardians",
                    Description = description.Trim(),
                    CreatedAtUtc = DateTime.UtcNow,
                    PerformedBy = "system"
                });

                await _db.SaveChangesAsync();

                

                await _db.SaveChangesAsync();


                await _notificationService.CreateNotificationForRolesAsync(
                    roleNames: new[] { "Admin", "Secretary" },
                    eventType: NotificationEvents.StudentActivity,
                    title: "Tutori cursant actualizați",
                    message: $"Tutorii cursantului {s.FullName} au fost actualizați.",
                    type: "Warning",
                    link: $"/students/{s.Id}",
                    entityType: "Student",
                    entityId: s.Id.ToString()
                );
            }

            return response.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> DeleteAsync(int id)
    {
        var response = new PublicResponse(true);

        try
        {
            var s = await _db.Students.FirstOrDefaultAsync(x => x.Id == id);
            if (s is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Student not found");

            s.IsActive = false;
            s.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = "Student",
                EntityId = s.Id.ToString(),
                Action = "Deactivate",
                Description = $"Cursantul {s.FullName} a fost dezactivat.",
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = "system"
            });

            await _db.SaveChangesAsync();

            await _notificationService.CreateNotificationForRolesAsync(
                 roleNames: new[] { "Admin", "Secretary" },
                 eventType: NotificationEvents.StudentActivity,
                 title: "Cursant dezactivat",
                 message: $"Cursantul {s.FullName} a fost dezactivat.",
                 type: "Warning",
                 link: $"/students/{s.Id}",
                 entityType: "Student",
                 entityId: s.Id.ToString()
             );

            return response.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<List<StudentOptionDto>> SearchOptionsAsync(string? q)
    {
        var query = _db.Students
            .AsNoTracking()
            .Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(s =>
                s.FullName.Contains(q) ||
                (s.Email != null && s.Email.Contains(q))
            );
        }

        return await query
            .OrderBy(s => s.FullName)
            .Take(20)
            .Select(s => new StudentOptionDto(
                s.Id,
                s.FullName,
                s.IsMinor
            ))
            .ToListAsync();
    }

    public async Task<PublicResponse> GetStudentCoursesAsync(int studentId)
    {
        var response = new PublicResponse(true);

        try
        {
            var enrollments = await _db.CourseEnrollments
                .AsNoTracking()
                .Include(e => e.Session)
                    .ThenInclude(s => s.Course)
                .Include(e => e.Session.Teacher)
                .Where(e => e.StudentId == studentId)
                .Select(e => new StudentCourseDetailsDto
                {
                    CourseId = e.CourseId,
                    CourseName = e.Session.Course.Name,

                    Price = e.Session.Fee,

                    SessionId = e.CourseSessionId,
                    DayOfWeek = e.Session.DayOfWeek.ToString(),
                    StartTime = e.Session.StartTime,
                    EndTime = e.Session.EndTime,

                    TeacherName = e.Session.Teacher.FirstName + " " +
                                  e.Session.Teacher.LastName,

                    // 🔥 NOU
                    IsActive = e.IsActive,
                    EndedAtUtc = e.EndedAtUtc,
                    ContractId = e.ContractId
                })
                .ToListAsync();

            var total = enrollments
                .Where(x => x.IsActive) // 🔥 doar active în total
                .Sum(c => c.Price);

            return response.SetSuccess(new
            {
                items = enrollments,
                totalAmount = total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetStudentCoursesAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> GetStudentCoursesByContractAsync(int contractId)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .Include(c => c.Parties)
            .FirstOrDefaultAsync(c => c.Id == contractId);

        if (contract == null)
            return response.SetError(ErrorCodes.InvalidParameters, "Contract not found");

        var studentId = contract.Parties
            .Where(p => p.StudentId != null)
            .Select(p => p.StudentId.Value)
            .FirstOrDefault();

        if (studentId == 0)
            return response.SetError(ErrorCodes.InvalidParameters, "Student not found");

        // 🔥 reutilizezi metoda ta existentă
        return await GetStudentCoursesAsync(studentId);
    }

    public async Task<PublicResponse> GetPrimaryGuardianAsync(int studentId)
    {
        var response = new PublicResponse(true);

        var studentExists = await _db.Students
            .AsNoTracking()
            .AnyAsync(s => s.Id == studentId);

        if (!studentExists)
            return response.SetError(ErrorCodes.InvalidParameters, "Student not found");

        var guardian = await _db.StudentGuardians
            .AsNoTracking()
            .Include(sg => sg.Guardian)
            .Where(sg => sg.StudentId == studentId && sg.IsPrimaryContact)
            .Select(sg => new GuardianOptionDto(
                sg.Guardian.Id,
                sg.Guardian.FirstName + " " + sg.Guardian.LastName
            ))
            .FirstOrDefaultAsync();

        return response.SetSuccess(guardian); // poate fi null
    }

    public async Task<List<AvailableCourseDto>> GetAvailableCoursesForStudentAsync(  int studentId,  string? q)
    {
        var query = _db.CourseSessions
            .Include(x => x.Course)
            .Include(x => x.Teacher)
            .Where(x => !x.Enrollments.Any(e => e.StudentId == studentId));

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(x => x.Course.Name.Contains(q));
        }

        return await query
            .Select(x => new AvailableCourseDto
            {
                CourseId = x.CourseId,
                SessionId = x.Id,
                CourseName = x.Course.Name,
                DayOfWeek = x.DayOfWeek,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                TeacherName = $"{x.Teacher.FirstName} {x.Teacher.LastName}",
                Price = x.Fee,
                Capacity = x.Capacity,
                Enrolled = x.Enrollments.Count()
            })
            .ToListAsync();
    }

    public async Task<PublicResponse> GetAllSessionsAsync()
    {
        var response = new PublicResponse(true);

        try
        {
            var sessions = await _db.CourseSessions
                .AsNoTracking()
                .Include(s => s.Course)
                .Include(s => s.Teacher)
                .Select(s => new SessionDto
                {
                    Id = s.Id,

                    CourseName = s.Course.Name,

                    TeacherName = s.Teacher.FirstName + " " + s.Teacher.LastName,

                    DayOfWeek = (DayOfWeek)s.DayOfWeek,
                    StartTime = s.StartTime.ToString("HH:mm"),
                    EndTime = s.EndTime.ToString("HH:mm"),

                    Fee = s.Fee,
                    IsActive = s.IsActive
                })
                .OrderBy(s => s.CourseName)
                .ToListAsync();

            return response.SetSuccess(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllSessionsAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<List<StudentListItemDto>> GetStudentsForExport( string? q, string? sortBy,  string? sortDir, bool? onlyRecent,  int? recentDays, int? sessionId)
    {
        var query = BuildStudentsQuery(q, sortBy, sortDir, onlyRecent, recentDays, sessionId);

        return await query
            .Select(s => new StudentListItemDto(
                s.Id,
                s.FullName,
                s.Email,
                s.Phone,
                s.IsActive,
                s.CreatedAtUtc
            ))
            .ToListAsync();
    }
   private IQueryable<ERPSystem.Data.Entities.Student> BuildStudentsQuery(string? q, string? sortBy, string? sortDir, bool? onlyRecent, int? recentDays, int? sessionId)
    {
        var query = _db.Students.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(s =>
                s.FullName.Contains(q) ||
                (s.Email != null && s.Email.Contains(q)) ||
                (s.Phone != null && s.Phone.Contains(q)));
        }

        if (onlyRecent == true)
        {
            var days = Math.Clamp(recentDays ?? 30, 1, 3650);
            var from = DateTime.UtcNow.AddDays(-days);
            query = query.Where(s => s.CreatedAtUtc >= from);
        }

        if (sessionId.HasValue)
        {
            query = query.Where(s => _db.CourseEnrollments
                .Any(e => e.StudentId == s.Id &&
                          e.CourseSessionId == sessionId.Value &&
                          e.IsActive));
        }

        var dirDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        query = (sortBy ?? "createdAt").ToLower() switch
        {
            "fullname" => dirDesc ? query.OrderByDescending(s => s.FullName) : query.OrderBy(s => s.FullName),
            _ => dirDesc ? query.OrderByDescending(s => s.CreatedAtUtc) : query.OrderBy(s => s.CreatedAtUtc)
        };

        return query;
    }

    public async Task<byte[]> ExportStudentsExcel( string? q, string? sortBy, string? sortDir, bool? onlyRecent, int? recentDays, int? sessionId)
    {
        var data = await GetStudentsForExport(q, sortBy, sortDir, onlyRecent, recentDays, sessionId);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Students");

        ws.Cell(1, 1).Value = "Nume";
        ws.Cell(1, 2).Value = "Email";
        ws.Cell(1, 3).Value = "Telefon";
        ws.Cell(1, 4).Value = "Status";
        ws.Cell(1, 5).Value = "Data";

        for (int i = 0; i < data.Count; i++)
        {
            var s = data[i];
            ws.Cell(i + 2, 1).Value = s.FullName;
            ws.Cell(i + 2, 2).Value = s.Email;
            ws.Cell(i + 2, 3).Value = s.Phone;
            ws.Cell(i + 2, 4).Value = s.IsActive ? "Activ" : "Inactiv";
            ws.Cell(i + 2, 5).Value = s.CreatedAtUtc.ToString("dd.MM.yyyy");
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

}
