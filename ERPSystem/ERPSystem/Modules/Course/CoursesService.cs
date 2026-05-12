using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Course.Models;
using ERPSystem.Shared.Notifications;
using ERPSystem.Utils.Constants.Email;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Shared.BusinessLogic;

public class CoursesService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CoursesService> _logger;
    private readonly NotificationsService _notificationService;
    private readonly ExcelExportService _excelExportService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CoursesService(
        ApplicationDbContext db, 
        UserManager<ApplicationUser> userManager, 
        ILogger<CoursesService> logger, 
        NotificationsService notificationService, 
        ExcelExportService excelExportService,
        IHttpContextAccessor httpContextAccessor)
     {
        _db = db;
        _userManager = userManager;
        _logger = logger;
        _notificationService = notificationService;
        _excelExportService = excelExportService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PublicResponse> ListAsync(string? q, string? status, string? deleteStatus, DiscountScope? scope)
    {
        var response = new PublicResponse(true);

        try
        {
            var query = _db.Courses.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(x => x.Name.Contains(q));
            }

            if (status == "active")
                query = query.Where(x => x.IsActive);

            if (status == "inactive")
                query = query.Where(x => !x.IsActive);

            if (string.IsNullOrWhiteSpace(deleteStatus) || deleteStatus == "notDeleted")
                query = query.Where(x => !x.IsDeleted);

            if (deleteStatus == "deleted")
                query = query.Where(x => x.IsDeleted);

            if (deleteStatus == "all")
                query = query;

            if (scope == DiscountScope.Package)
            {
                query = query.Where(c =>
                    c.Sessions.Any(s => s.FeeType == CourseFeeType.FixedPackage));
            }

            if (scope == DiscountScope.Subscription)
            {
                query = query.Where(c =>
                    c.Sessions.Any(s => s.FeeType == CourseFeeType.Monthly));
            }

            var items = await query
                .OrderBy(x => x.IsDeleted)
                .ThenByDescending(x => x.IsActive)
                .ThenBy(x => x.Name)
                .Select(x => new CourseListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsActive = x.IsActive,
                    IsDeleted = x.IsDeleted,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToListAsync();

            return response.SetSuccess(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> GetAsync(int id)
    {
        var response = new PublicResponse(true);

        try
        {
            var c = await _db.Courses.AsNoTracking()
                .Include(x => x.Sessions)
                    .ThenInclude(s => s.Teacher)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Cursul nu a fost găsit.");

            var sessionCounts = await _db.CourseEnrollments.AsNoTracking()
                .Where(e => e.CourseId == id && e.IsActive)
                .GroupBy(e => e.CourseSessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SessionId, x => x.Count);

            var sessions = c.Sessions
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .Select(s => new CourseSessionDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime.ToString("HH:mm"),
                    EndTime = s.EndTime.ToString("HH:mm"),
                    Capacity = s.Capacity,
                    EnrolledActiveCount = sessionCounts.TryGetValue(s.Id, out var cnt) ? cnt : 0,
                    TeacherUserId = s.TeacherUserId,
                    TeacherName = s.Teacher.UserName ?? s.Teacher.Email ?? s.TeacherUserId,

                    Fee = s.Fee,
                    FeeType = s.FeeType,              
                    TotalSessions = s.TotalSessions,
                    IsActive = s.IsActive
                })
                .ToList();

            var dto = new CourseDetailsDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAtUtc = c.CreatedAtUtc,

                DeletedAtUtc = c.DeletedAtUtc,
                UpdatedAtUtc = c.UpdatedAtUtc,

                Sessions = sessions
            };

            return response.SetSuccess(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> CreateAsync(CreateCourseDto dto)
    {
        var response = new PublicResponse(true);

        try
        {
            var validation = ValidateCreate(dto);
            if (validation is not null)
                return response.SetError(ErrorCodes.InvalidParameters, validation);

            if (HasTeacherOverlap(dto.Sessions))
                return response.SetError(
                    ErrorCodes.InvalidParameters,
                    "Profesorul are sesiuni suprapuse în aceeași zi."
                );

            if (await HasTeacherOverlapInDatabase(dto.Sessions))
            {
                return response.SetError(
                    ErrorCodes.InvalidParameters,
                    "Profesorul are deja o sesiune suprapusă în alt curs."
                );
            }
            var c = new Course
            {
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            foreach (var s in dto.Sessions)
            {
                c.Sessions.Add(new CourseSession
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = ParseTime(s.StartTime),
                    EndTime = ParseTime(s.EndTime),
                    Capacity = s.Capacity,
                    TeacherUserId = s.TeacherUserId,

                    FeeType = s.FeeType,
                    Fee = s.Fee,
                    TotalSessions = s.FeeType == CourseFeeType.FixedPackage  ? s.TotalSessions : null,

                    Title = $"{dto.Name} - {GetRomanianDay(s.DayOfWeek)}"
                });
            }

            _db.Courses.Add(c);
            await _db.SaveChangesAsync();

            var teacherIds = c.Sessions
                .Select(s => s.TeacherUserId)
                .Distinct()
                .ToList();

            var teachers = await _db.Users
                .Where(u => teacherIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? u.Id);

            var sessionDescriptions = c.Sessions.Select(s =>
            {
                var teacherName = teachers.ContainsKey(s.TeacherUserId)
                    ? teachers[s.TeacherUserId]
                    : s.TeacherUserId;

                return $"- {s.DayOfWeek} {s.StartTime:HH:mm}-{s.EndTime:HH:mm} ({teacherName})";
            }).ToList();

            var description = $"Cursul '{c.Name}' a fost creat";

            if (sessionDescriptions.Any())
            {
                description += "\nSesiuni:\n" + string.Join("\n", sessionDescriptions);
            }

            _db.ActivityLog.Add(new ERPSystem.Data.Entities.ActivityLog
            {
                EntityType = "Course",
                EntityId = c.Id.ToString(),
                Action = "Create",
                Description = description,
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = GetCurrentUser()
            });

            foreach (var session in c.Sessions)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: session.TeacherUserId,
                    eventType: NotificationEvents.CourseActivity,
                    title: "Ai fost asignat la un curs",
                    message: $"Ai fost asignat la cursul '{c.Name}', sesiunea {GetRomanianDay(session.DayOfWeek)} {session.StartTime:HH:mm}-{session.EndTime:HH:mm}.",
                    type: "Info",
                    link: "/courses",
                    entityType: "Course",
                    entityId: c.Id.ToString()
                );
            }
            return response.SetCreated(new { id = c.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> UpdateAsync(int id, UpdateCourseDto dto)
    {
        var response = new PublicResponse(true);

        try
        {
            var validation = ValidateUpdate(dto);
            if (validation is not null)
                return response.SetError(ErrorCodes.InvalidParameters, validation);

            if (HasTeacherOverlap(dto.Sessions))
                return response.SetError(
                    ErrorCodes.InvalidParameters,
                    "Profesorul are sesiuni suprapuse în aceeași zi."
                );

            if (await HasTeacherOverlapInDatabase(dto.Sessions, id))
            {
                return response.SetError(
                    ErrorCodes.InvalidParameters,
                    "Profesorul are deja o sesiune suprapusă în alt curs."
                );
            }

            var c = await _db.Courses
                .Include(x => x.Sessions)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Cursul nu a fost gasit.");

            if (!c.IsActive)
                return response.SetError(ErrorCodes.InvalidParameters, "Nu poți modifica un curs inactiv.");

            if (!dto.IsActive)
            {
                foreach (var session in c.Sessions)
                    session.IsActive = false;
            }

            var oldName = c.Name;
            var oldIsActive = c.IsActive;

            var oldSessions = c.Sessions.ToDictionary(
                  s => s.Id,
                  s => new
                  {
                      s.DayOfWeek,
                      s.StartTime,
                      s.EndTime,
                      s.TeacherUserId,
                      s.Capacity,
                      s.Fee,
                      s.FeeType,
                      s.TotalSessions
                  });

            c.Name = dto.Name.Trim();
            c.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            c.IsActive = dto.IsActive;

            if (!dto.IsActive)
            {
                foreach (var session in c.Sessions)
                    session.IsActive = false;
            }

            c.UpdatedAtUtc = DateTime.UtcNow;

            var incomingIds = dto.Sessions
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .ToHashSet();

            var removedSessions = c.Sessions
                .Where(s => !incomingIds.Contains(s.Id))
                .ToList();

            c.Sessions.RemoveAll(s => !incomingIds.Contains(s.Id));

            var addedSessions = new List<CourseSession>();

            foreach (var sDto in dto.Sessions)
            {
                if (sDto.Id is null)
                {
                    var newSession = new CourseSession
                    {
                        DayOfWeek = sDto.DayOfWeek,
                        StartTime = ParseTime(sDto.StartTime),
                        EndTime = ParseTime(sDto.EndTime),
                        Capacity = sDto.Capacity,
                        TeacherUserId = sDto.TeacherUserId,
                        Fee = sDto.Fee,
                        FeeType = sDto.FeeType,
                        TotalSessions = sDto.FeeType == CourseFeeType.FixedPackage  ? sDto.TotalSessions : null,
                        Title = $"{dto.Name} - {GetRomanianDay(sDto.DayOfWeek)}"
                    };

                    c.Sessions.Add(newSession);
                    addedSessions.Add(newSession);
                }
                else
                {
                    var existing = c.Sessions.FirstOrDefault(x => x.Id == sDto.Id.Value);
                    if (existing is null) continue;

                    existing.DayOfWeek = sDto.DayOfWeek;
                    existing.StartTime = ParseTime(sDto.StartTime);
                    existing.EndTime = ParseTime(sDto.EndTime);
                    existing.Capacity = sDto.Capacity;
                    existing.TeacherUserId = sDto.TeacherUserId;
                    existing.Fee = sDto.Fee;
                    existing.FeeType = sDto.FeeType;
                    existing.Title = $"{dto.Name} - {GetRomanianDay(sDto.DayOfWeek)}";
                    existing.TotalSessions = sDto.FeeType == CourseFeeType.FixedPackage
                        ? sDto.TotalSessions
                        : null;
                }

            }

            await _db.SaveChangesAsync(); 

            var teacherIds = dto.Sessions
                .Select(x => x.TeacherUserId)
                .Concat(oldSessions.Values.Select(x => x.TeacherUserId))
                .Distinct()
                .ToList();

            var teachers = await _db.Users
                .Where(u => teacherIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? u.Id);

            var changes = new List<string>();

            if (oldName != c.Name)
                changes.Add($"Nume: '{oldName}' → '{c.Name}'");

            if (oldIsActive != c.IsActive)
                changes.Add(c.IsActive ? "Curs activat" : "Curs dezactivat");

            
            if (addedSessions.Any())
            {
                var added = addedSessions.Select(s =>
                {
                    var teacherName = teachers.ContainsKey(s.TeacherUserId)
                        ? teachers[s.TeacherUserId]
                        : s.TeacherUserId;

                    return $"{s.DayOfWeek} {s.StartTime:HH:mm}-{s.EndTime:HH:mm} ({teacherName})";
                });

                changes.Add("Sesiuni adăugate: " + string.Join(", ", added));
            }

            if (removedSessions.Any())
            {
                var removed = removedSessions.Select(s =>
                {
                    var teacherName = teachers.ContainsKey(s.TeacherUserId)
                        ? teachers[s.TeacherUserId]
                        : s.TeacherUserId;

                    return $"{s.DayOfWeek} {s.StartTime:HH:mm}-{s.EndTime:HH:mm} ({teacherName})";
                });

                changes.Add("Sesiuni eliminate: " + string.Join(", ", removed));
            }

            var updatedSessions = new List<string>();

            foreach (var sDto in dto.Sessions)
            {
                if (sDto.Id is null) continue;

                var idSession = sDto.Id.Value;
                if (!oldSessions.ContainsKey(idSession)) continue;

                var old = oldSessions[idSession];

                var newStart = ParseTime(sDto.StartTime);
                var newEnd = ParseTime(sDto.EndTime);

                var sessionChanges = new List<string>();

                if (old.StartTime != newStart || old.EndTime != newEnd)
                {
                    sessionChanges.Add(
                        $"Ora: {old.StartTime:HH:mm}-{old.EndTime:HH:mm} → {newStart:HH:mm}-{newEnd:HH:mm}"
                    );
                }

                if (old.DayOfWeek != sDto.DayOfWeek)
                {
                    sessionChanges.Add($"Zi: {old.DayOfWeek} → {sDto.DayOfWeek}");
                }

                if (old.TeacherUserId != sDto.TeacherUserId)
                {
                    var oldTeacher = teachers.ContainsKey(old.TeacherUserId) ? teachers[old.TeacherUserId] : old.TeacherUserId;
                    var newTeacher = teachers.ContainsKey(sDto.TeacherUserId) ? teachers[sDto.TeacherUserId] : sDto.TeacherUserId;

                    sessionChanges.Add($"Profesor: {oldTeacher} → {newTeacher}");
                }

                if (old.Capacity != sDto.Capacity)
                {
                    sessionChanges.Add($"Capacitate: {old.Capacity} → {sDto.Capacity}");
                }

                if (sessionChanges.Any())
                {
                    updatedSessions.Add(
                        $"{old.DayOfWeek} {old.StartTime:HH:mm}: {string.Join(", ", sessionChanges)}"
                    );
                }

                if (old.Fee != sDto.Fee)
                {
                    sessionChanges.Add($"Preț: {old.Fee} → {sDto.Fee}");
                }

                if (old.FeeType != sDto.FeeType)
                {
                    sessionChanges.Add($"Tip: {old.FeeType} → {sDto.FeeType}");
                }

                if (old.TotalSessions != sDto.TotalSessions)
                {
                    sessionChanges.Add($"Ședințe: {old.TotalSessions} → {sDto.TotalSessions}");
                }

            }

            if (updatedSessions.Any())
            {
                changes.Add("Sesiuni modificate:");
                changes.AddRange(updatedSessions.Select(x => "- " + x));
            }

            if (changes.Any())
            {
                _db.ActivityLog.Add(new ActivityLog
                {
                    EntityType = "Course",
                    EntityId = c.Id.ToString(),
                    Action = "Update",
                    Description = $"Curs actualizat:\n{string.Join("\n", changes)}",
                    CreatedAtUtc = DateTime.UtcNow,
                    PerformedBy = GetCurrentUser()
                });

                await _db.SaveChangesAsync();
            }

            var affectedTeacherIds = dto.Sessions
                .Select(x => x.TeacherUserId)
                .Concat(oldSessions.Values.Select(x => x.TeacherUserId))
                .Distinct()
                .ToList();
              
            foreach (var teacherUserId in affectedTeacherIds)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: teacherUserId,
                    eventType: NotificationEvents.CourseActivity,
                    title: "Curs actualizat",
                    message: $"Cursul '{c.Name}' a fost actualizat.",
                    type: "Warning",
                    link: "/courses",
                    entityType: "Course",
                    entityId: c.Id.ToString()
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

    public async Task<PublicResponse> ToggleCourseStatusAsync(int id)
    {
        var response = new PublicResponse(true);

        try
        {
            var course = await _db.Courses
                .Include(x => x.Sessions)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (course == null)
                return response.SetError("NOT_FOUND", "Cursul nu a fost găsit.");

            var wasActive = course.IsActive;

            course.IsActive = !course.IsActive;

            if (!course.IsActive)
            {
                foreach (var session in course.Sessions)
                    session.IsActive = false;
            }
            else
            {
                foreach (var session in course.Sessions)
                    session.IsActive = true;
            }

            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = "Course",
                EntityId = course.Id.ToString(),
                Action = course.IsActive ? "CourseActivated" : "CourseDeactivated",
                Description = course.IsActive
                    ? $"Cursul '{course.Name}' a fost activat."
                    : $"Cursul '{course.Name}' a fost dezactivat.",
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = GetCurrentUser()
            });

            await _db.SaveChangesAsync();

            var teacherIds = course.Sessions
                .Select(x => x.TeacherUserId)
                .Distinct()
                .ToList();

            foreach (var teacherUserId in teacherIds)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: teacherUserId,
                    eventType: NotificationEvents.CourseActivity,
                    title: course.IsActive ? "Curs activat" : "Curs dezactivat",
                    message: course.IsActive
                        ? $"Cursul '{course.Name}' a fost activat."
                        : $"Cursul '{course.Name}' a fost dezactivat.",
                    type: course.IsActive ? "Success" : "Warning",
                    link: "/courses",
                    entityType: "Course",
                    entityId: course.Id.ToString()
                );
            }

            return response.SetSuccess(new
            {
                course.Id,
                course.IsActive
            });
        }
        catch (Exception ex)
        {
            return response.SetError("SERVER", ex.Message);
        }
    }

    public async Task<PublicResponse> DeleteAsync(int id)
    {
        var response = new PublicResponse(true);

        try
        {
            var c = await _db.Courses
              .Include(x => x.Sessions)
              .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            
            if (c is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Cursul nu a fost găsit.");

            c.IsDeleted = true;

            c.DeletedAtUtc = DateTime.UtcNow;

            c.IsActive = false;

            foreach (var s in c.Sessions)
                s.IsActive = false;

            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = "Course",
                EntityId = c.Id.ToString(),
                Action = "Delete",
                Description = $"Cursul '{c.Name}' a fost șters.",
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = GetCurrentUser()
            });

            await _db.SaveChangesAsync();

            var teacherIds = c.Sessions
               .Select(x => x.TeacherUserId)
               .Distinct()
               .ToList();

            foreach (var teacherUserId in teacherIds)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: teacherUserId,
                    eventType: NotificationEvents.CourseActivity,
                    title: "Curs șters",
                    message: $"Cursul '{c.Name}' a fost șters.",
                    type: "Warning",
                    link: "/courses",
                    entityType: "Course",
                    entityId: c.Id.ToString()
                );
            }

            return response.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> RestoreAsync(int id)
    {
        var response = new PublicResponse(true);

        var c = await _db.Courses
            .Include(x => x.Sessions)
            .FirstOrDefaultAsync(x => x.Id == id);
          
        if (c is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Cursul nu a fost găsit.");

        c.IsDeleted = false;
        c.DeletedAtUtc = null;
        c.IsActive = false;

        await _db.SaveChangesAsync();

        var teacherIds = c.Sessions
            .Select(x => x.TeacherUserId)
            .Distinct()
            .ToList();

        foreach (var teacherUserId in teacherIds)
        {
            await _notificationService.CreateNotificationAsync(
                userId: teacherUserId,
                eventType: NotificationEvents.CourseActivity,
                title: "Curs restaurat",
                message: $"Cursul '{c.Name}' a fost restaurat.",
                type: "Success",
                link: "/courses",
                entityType: "Course",
                entityId: c.Id.ToString()
            );
        }

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> ToggleSessionStatusAsync(int id)
    {
        var response = new PublicResponse(true);

        try
        {
            var session = await _db.CourseSessions
                .Include(x => x.Enrollments)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (session == null)
                return response.SetError("NOT_FOUND", "Sesiunea nu a fost găsită");

            if (session.IsActive)
            {
                var hasActiveEnrollments = session.Enrollments.Any(e => e.IsActive);

                if (hasActiveEnrollments)
                    return response.SetError("BUSINESS_RULE",
                        "Nu poți dezactiva sesiunea. Există cursanți activi.");
            }

            session.IsActive = !session.IsActive;

            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = "CourseSession",
                EntityId = session.Id.ToString(),
                Action = session.IsActive ? "SessionActivated" : "SessionDeactivated",
                Description = session.IsActive
                    ? $"Sesiunea {session.Title} a fost activată."
                    : $"Sesiunea {session.Title} a fost dezactivată.",
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = GetCurrentUser()
            });

            await _db.SaveChangesAsync();

            if (!string.IsNullOrEmpty(session.TeacherUserId))
            {
                await _notificationService.CreateNotificationAsync(
                    userId: session.TeacherUserId,
                    eventType: NotificationEvents.CourseActivity,
                    title: session.IsActive ? "Sesiune activată" : "Sesiune dezactivată",
                    message: session.IsActive
                        ? "Sesiunea ta a fost activată."
                        : "Sesiunea ta a fost dezactivată.",
                    type: session.IsActive ? "Success" : "Warning",
                    link: "/courses",
                    entityType: "CourseSession",
                    entityId: session.Id.ToString()
                );
            }

            return response.SetSuccess(new
            {
                session.Id,
                session.IsActive
            });
        }
        catch (Exception ex)
        {
            return response.SetError("SERVER", ex.Message);
        }
    }

    public async Task<PublicResponse> ListEnrollmentsAsync(int courseId, int? sessionId = null)
    {
        var response = new PublicResponse(true);

        try
        {
            var query = _db.CourseEnrollments.AsNoTracking()
                .Include(x => x.Student)
                .Include(x => x.Session)
                .Where(x => x.CourseId == courseId);

            if (sessionId.HasValue)
            {
                query = query.Where(x => x.CourseSessionId == sessionId.Value);
            }

            var items = await query
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Student.FullName)
                .Select(x => new EnrollmentDto
                {
                    StudentId = x.StudentId,
                    StudentName = x.Student.FullName,
                    StudentEmail = x.Student.Email,
                    EnrolledAtUtc = x.EnrolledAtUtc,
                    IsActive = x.IsActive,
                    SessionId = x.CourseSessionId,
                    DayOfWeek = x.Session.DayOfWeek,
                    StartTime = x.Session.StartTime.ToString("HH:mm"),
                    EndTime = x.Session.EndTime.ToString("HH:mm"),
                    UnenrolledAtUtc = x.EndedAtUtc,

                    FeedbackSent = _db.EmailRecipientLogs.Any(r =>
                        r.StudentId == x.StudentId &&
                        r.IsSent &&
                        r.EmailLog.Type == EmailLogTypes.FeedbackForm &&
                        r.EmailLog.ReferenceId == x.CourseSessionId),

                    FeedbackSentAt = _db.EmailRecipientLogs
                   .Where(r =>
                       r.StudentId == x.StudentId &&
                       r.IsSent &&
                       r.EmailLog.Type == EmailLogTypes.FeedbackForm &&
                       r.EmailLog.ReferenceId == x.CourseSessionId)
                   .OrderByDescending(r => r.SentAt)
                   .Select(r => r.SentAt)
                   .FirstOrDefault()
                })
                .ToListAsync();

            return response.SetSuccess(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListEnrollmentsAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> EnrollStudentAsync(int courseId, int studentId, int sessionId)
    {
        var response = new PublicResponse(true);

        try
        {
            var course = await _db.Courses.FindAsync(courseId);
            if (course is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Cursul nu a fost găsit.");

            var student = await _db.Students.FindAsync(studentId);
            if (student is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Cursantul nu a fost găsit.");

            if (!course.IsActive)
                return response.SetError(ErrorCodes.InvalidParameters, "Cursul este inactiv.");

            var session = await _db.CourseSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.CourseId == courseId);

            if (session is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Sesiunea nu a fost găsită pentru acest curs.");

            if (!session.IsActive)
                return response.SetError(ErrorCodes.InvalidParameters, "Sesiunea este inactivă.");

            if (session.Capacity.HasValue)
            {
                var activeCount = await _db.CourseEnrollments
                    .CountAsync(e => e.CourseSessionId == sessionId && e.IsActive);

                if (activeCount >= session.Capacity.Value)
                    return response.SetError(ErrorCodes.InvalidParameters, "Sesiunea a atins limita de cursanți.");
            }

            var alreadyActive = await _db.CourseEnrollments
               .AnyAsync(x => x.CourseSessionId == sessionId && x.StudentId == studentId && x.IsActive);

            if (alreadyActive)
                return response.SetError(ErrorCodes.InvalidParameters, "Cursantul este deja înscris activ la această sesiune.");

            _db.CourseEnrollments.Add(new CourseEnrollment
            {
                CourseId = courseId,
                CourseSessionId = sessionId,
                StudentId = studentId,
                EnrolledAtUtc = DateTime.UtcNow,
                IsActive = true
            });

            var sessionInfo = $"{GetRomanianDay(session.DayOfWeek)} {session.StartTime:HH:mm}";

            var description = $"Studentul {student.FullName} a fost înscris la cursul {course.Name} ({sessionInfo})";
              
            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = "Student",
                EntityId = studentId.ToString(),
                Action =  "Enroll",
                Description = description,
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = GetCurrentUser()
            });

            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = "Course",
                EntityId = courseId.ToString(),
                Action =  "EnrollStudent",
                Description = description,
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = GetCurrentUser()
            });

            await _db.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                userId: session.TeacherUserId,
                eventType: NotificationEvents.CourseActivity,
                title: "Cursant înscris",
                message: $"Cursantul {student.FullName} a fost înscris la cursul '{course.Name}', sesiunea {GetRomanianDay(session.DayOfWeek)} {session.StartTime:HH:mm}.",
                type: "Success",
                link: "/courses",
                entityType: "CourseEnrollment",
                entityId: sessionId.ToString()
            );

            return response.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EnrollStudentAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> SetEnrollmentActiveAsync(int courseId, int sessionId, int studentId, bool isActive)
    {
        var response = new PublicResponse(true);

        try
        {     
            var existing = await _db.CourseEnrollments
              .FirstOrDefaultAsync(x => x.CourseId == courseId && x.CourseSessionId == sessionId && x.StudentId == studentId);

            if (!isActive)
            {
                if (existing is null)
                    return response.SetError(ErrorCodes.InvalidParameters, "Enrollment not found");

                existing.IsActive = false;
                existing.EndedAtUtc = DateTime.UtcNow;

                var student = await _db.Students.FindAsync(studentId);
                var course = await _db.Courses.FindAsync(courseId);
                var session = await _db.CourseSessions.FindAsync(sessionId);

                var sessionInfo = $"{GetRomanianDay(session.DayOfWeek)} {session.StartTime:HH:mm}";

                var description = $"Studentul {student!.FullName} a fost eliminat din cursul {course!.Name} ({sessionInfo})";

                _db.ActivityLog.Add(new ActivityLog
                {
                    EntityType = "Student",
                    EntityId = studentId.ToString(),
                    Action = "EnrollDeactivate",
                    Description = description,
                    CreatedAtUtc = DateTime.UtcNow,
                    PerformedBy = GetCurrentUser()
                });

                _db.ActivityLog.Add(new ActivityLog
                {
                    EntityType = "Course",
                    EntityId = courseId.ToString(),
                    Action = "StudentRemoved",
                    Description = description,
                    CreatedAtUtc = DateTime.UtcNow,
                    PerformedBy = GetCurrentUser()
                });

                await _db.SaveChangesAsync();

                await _notificationService.CreateNotificationAsync(
                    userId: session!.TeacherUserId,
                    eventType: NotificationEvents.CourseActivity,
                    title: "Cursant eliminat",
                    message: $"Cursantul {student!.FullName} a fost eliminat din cursul '{course!.Name}', sesiunea {GetRomanianDay(session.DayOfWeek)} {session.StartTime:HH:mm}.",
                    type: "Warning",
                    link: "/courses",
                    entityType: "CourseEnrollment",
                    entityId: existing.Id.ToString()
                );
                return response.SetSuccess(true); 

            }
            else
            {
                return await EnrollStudentAsync(courseId, studentId, sessionId);
            }
           
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetEnrollmentActiveAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> GetTeachersAsync()
    {
        var response = new PublicResponse(true);

        try
        {
            var roleId = await _db.Roles
                .Where(r => r.Name == "Teacher")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(roleId))
                return response.SetSuccess(new List<TeacherOptionDto>());

            var teachers = await (
                from ur in _db.UserRoles
                join u in _db.Users on ur.UserId equals u.Id
                where ur.RoleId == roleId
                orderby u.UserName
                select new TeacherOptionDto
                {
                    UserId = u.Id,                          
                    DisplayName = u.UserName ?? u.Email    
                }
            ).ToListAsync();

            return response.SetSuccess(teachers);
        }
        catch (Exception)
        {
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }
   
    private static string? ValidateCreate(CreateCourseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Numele cursului este obligatoriu.";

        if (dto.Sessions is null || dto.Sessions.Count == 0)
            return "Cursul trebuie să aibă cel puțin o sesiune.";

        foreach (var s in dto.Sessions)
        {
            if (string.IsNullOrWhiteSpace(s.TeacherUserId))
                return "Fiecare sesiune trebuie să aibă un profesor.";
        }
        foreach (var s in dto.Sessions)
        {
            if (s.FeeType == CourseFeeType.FixedPackage)
            {
                if (!s.TotalSessions.HasValue || s.TotalSessions <= 0)
                    return "Numarul sesiunilor este obligatoriu pentru pachet fix.";
            }

            if (s.FeeType == CourseFeeType.Monthly)
            {
                if (s.TotalSessions != null)
                    return "Numarul sesiunilor nu este permis pentru abonament.";
            }

            if (s.Fee <= 0)
                return "Pretul trebuie să fie mai mare decât 0.";
        }

        return null;
    }

    private static string? ValidateUpdate(UpdateCourseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Numele cursului este obligatoriu.";

        if (dto.Sessions is null || dto.Sessions.Count == 0)
            return "Cursul trebuie să aibă cel puțin o sesiune.";

        foreach (var s in dto.Sessions)
        {
            if (string.IsNullOrWhiteSpace(s.TeacherUserId))
                return "Fiecare sesiune trebuie să aibă un profesor.";
        }
        foreach (var s in dto.Sessions)
        {
            if (s.FeeType == CourseFeeType.FixedPackage)
            {
                if (!s.TotalSessions.HasValue || s.TotalSessions <= 0)
                    return "Numarul sesiunilor este obligatoriu pentru pachet fix.";
            }

            if (s.FeeType == CourseFeeType.Monthly)
            {
                if (s.TotalSessions != null)
                    return "Numarul sesiunilor nu este permis pentru abonament.";
            }

            if (s.Fee <= 0)
                return "Pretul trebuie să fie mai mare decât 0.";
        }

        return null;
    }

    private static bool HasTeacherOverlap(List<CourseSessionUpsertDto> sessions)
    {
        for (int i = 0; i < sessions.Count; i++)
        {
            for (int j = i + 1; j < sessions.Count; j++)
            {
                var a = sessions[i];
                var b = sessions[j];

                if (a.TeacherUserId != b.TeacherUserId)
                    continue;

                if (a.DayOfWeek != b.DayOfWeek)
                    continue;

                var startA = ParseTime(a.StartTime);
                var endA = ParseTime(a.EndTime);
                var startB = ParseTime(b.StartTime);
                var endB = ParseTime(b.EndTime);

                if (startA < endB && startB < endA)
                    return true;
            }
        }

        return false;
    }

    private async Task<bool> HasTeacherOverlapInDatabase(  List<CourseSessionUpsertDto> sessions, int? currentCourseId = null)
    {
        foreach (var s in sessions)
        {
            var start = ParseTime(s.StartTime);
            var end = ParseTime(s.EndTime);

            var query = _db.CourseSessions
                .Where(x =>
                    x.TeacherUserId == s.TeacherUserId &&
                    x.DayOfWeek == s.DayOfWeek &&
                    start < x.EndTime &&
                    x.StartTime < end
                );

            if (currentCourseId.HasValue)
            {
                query = query.Where(x => x.CourseId != currentCourseId.Value);
            }

            var exists = await query.AnyAsync();

            if (exists)
                return true;
        }

        return false;
    }

    public async Task<PublicResponse> GetAvailableStudentsAsync(  int courseId,  int sessionId,  string? q)
    {
        var response = new PublicResponse(true);

        try
        {
            var sessionExists = await _db.CourseSessions
                .AnyAsync(s =>
                    s.Id == sessionId &&
                    s.CourseId == courseId &&
                    s.IsActive &&
                    s.Course.IsActive);

            if (!sessionExists)
                return response.SetError(
                    ErrorCodes.InvalidParameters,
                    "Sesiunea nu a fost gasita sau este inactiva"
                );

            var enrolledIds = await _db.CourseEnrollments
                .Where(e =>
                    e.CourseSessionId == sessionId &&
                    e.IsActive)
                .Select(e => e.StudentId)
                .ToListAsync();

            var query = _db.Students
                .AsNoTracking()
                .Where(s =>
                    s.IsActive &&
                    !s.IsDeleted &&
                    !enrolledIds.Contains(s.Id));

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();

                query = query.Where(s =>
                    s.FullName.Contains(q) ||
                    (s.Email != null && s.Email.Contains(q))
                );
            }

            var students = await query
                .OrderBy(s => s.FullName)
                .Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.Email
                })
                .ToListAsync();

            return response.SetSuccess(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAvailableStudentsAsync failed");

            return response.SetError(
                ErrorCodes.InternalServerError,
                ErrorMessages.InternalServerError
            );
        }
    }

    public async Task<IResult> ExportCoursesExcelAsync( string? q,string? status, string? deleteStatus, DiscountScope? scope)
    {
        var query = _db.Courses
            .Include(x => x.Sessions)
                .ThenInclude(x => x.Teacher)
            .Include(x => x.Sessions)
                .ThenInclude(x => x.Enrollments)
            .Include(x => x.Enrollments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(x => x.Name.Contains(q));
        }

        if (status == "active")
            query = query.Where(x => x.IsActive);

        if (status == "inactive")
            query = query.Where(x => !x.IsActive);

        if (string.IsNullOrWhiteSpace(deleteStatus) || deleteStatus == "notDeleted")
            query = query.Where(x => !x.IsDeleted);

        if (deleteStatus == "deleted")
            query = query.Where(x => x.IsDeleted);

        if (scope == DiscountScope.Package)
        {
            query = query.Where(c =>
                c.Sessions.Any(s => s.FeeType == CourseFeeType.FixedPackage));
        }

        if (scope == DiscountScope.Subscription)
        {
            query = query.Where(c =>
                c.Sessions.Any(s => s.FeeType == CourseFeeType.Monthly));
        }

        var courses = await query
            .OrderBy(x => x.IsDeleted)
            .ThenByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .ToListAsync();

        using var wb = new XLWorkbook();

        var coursesWs = wb.Worksheets.Add("Cursuri");
        var sessionsWs = wb.Worksheets.Add("Sesiuni");

        var courseHeaders = new[]
        {
        "ID", "Nume curs", "Descriere", "Status", "Șters",
        "Nr. sesiuni", "Sesiuni active",
        "Nr. înscrieri", "Înscrieri active",
        "Creat la", "Actualizat la", "Șters la"
    };

        for (int i = 0; i < courseHeaders.Length; i++)
            coursesWs.Cell(1, i + 1).Value = courseHeaders[i];

        var row = 2;

        foreach (var c in courses)
        {
            coursesWs.Cell(row, 1).Value = c.Id;
            coursesWs.Cell(row, 2).Value = c.Name;
            coursesWs.Cell(row, 3).Value = c.Description ?? "";
            coursesWs.Cell(row, 4).Value = c.IsActive ? "Activ" : "Inactiv";
            coursesWs.Cell(row, 5).Value = c.IsDeleted ? "Da" : "Nu";
            coursesWs.Cell(row, 6).Value = c.Sessions.Count;
            coursesWs.Cell(row, 7).Value = c.Sessions.Count(s => s.IsActive);
            coursesWs.Cell(row, 8).Value = c.Enrollments.Count;
            coursesWs.Cell(row, 9).Value = c.Enrollments.Count(e => e.IsActive);
            coursesWs.Cell(row, 10).Value = c.CreatedAtUtc;
            coursesWs.Cell(row, 11).Value = c.UpdatedAtUtc;
            coursesWs.Cell(row, 12).Value = c.DeletedAtUtc;

            row++;
        }

        var sessionHeaders = new[]
        {
        "ID sesiune", "ID curs", "Nume curs", "Titlu sesiune",
        "Profesor", "Tip taxă", "Taxă", "Total ședințe",
        "Zi", "Ora început", "Ora sfârșit",
        "Capacitate", "Înscriși", "Locuri libere", "Status"
    };

        for (int i = 0; i < sessionHeaders.Length; i++)
            sessionsWs.Cell(1, i + 1).Value = sessionHeaders[i];

        row = 2;

        foreach (var c in courses)
        {
            var sessions = c.Sessions.AsEnumerable();

            if (scope == DiscountScope.Package)
                sessions = sessions.Where(s => s.FeeType == CourseFeeType.FixedPackage);

            if (scope == DiscountScope.Subscription)
                sessions = sessions.Where(s => s.FeeType == CourseFeeType.Monthly);

            foreach (var s in sessions.OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime))
            {
                var activeEnrollments = s.Enrollments.Count(e => e.IsActive);

                var freeSeats = s.Capacity.HasValue
                    ? Math.Max(s.Capacity.Value - activeEnrollments, 0)
                    : (int?)null;

                sessionsWs.Cell(row, 1).Value = s.Id;
                sessionsWs.Cell(row, 2).Value = c.Id;
                sessionsWs.Cell(row, 3).Value = c.Name;
                sessionsWs.Cell(row, 4).Value = s.Title;
                sessionsWs.Cell(row, 5).Value = s.Teacher?.FullName ?? s.TeacherUserId;
                sessionsWs.Cell(row, 6).Value = s.FeeType == CourseFeeType.FixedPackage ? "Pachet" : "Abonament";
                sessionsWs.Cell(row, 7).Value = s.Fee;
                sessionsWs.Cell(row, 8).Value = s.TotalSessions;
                sessionsWs.Cell(row, 9).Value = GetRomanianDay(s.DayOfWeek);
                sessionsWs.Cell(row, 10).Value = s.StartTime.ToString("HH:mm");
                sessionsWs.Cell(row, 11).Value = s.EndTime.ToString("HH:mm");
                sessionsWs.Cell(row, 12).Value = s.Capacity?.ToString() ?? "Nelimitat";
                sessionsWs.Cell(row, 13).Value = activeEnrollments;
                sessionsWs.Cell(row, 14).Value = freeSeats?.ToString() ?? "Nelimitat";
                sessionsWs.Cell(row, 15).Value = s.IsActive ? "Activ" : "Inactiv";

                row++;
            }
        }

        _excelExportService.FormatSheet(coursesWs);
        _excelExportService.FormatSheet(sessionsWs);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);

        return Results.File(
            ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"cursuri_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx"
        );
    }

    private static string GetRomanianDay(int day)
    {
        return day switch
        {
            1 => "Luni",
            2 => "Marți",
            3 => "Miercuri",
            4 => "Joi",
            5 => "Vineri",
            6 => "Sâmbătă",
            7 => "Duminică",
            _ => "-"
        };
    }

    private static TimeOnly ParseTime(string s)
    {
        if (!TimeOnly.TryParse(s, out var t))
            throw new ArgumentException($"Invalid time: {s}. Use HH:mm");
        return t;
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
