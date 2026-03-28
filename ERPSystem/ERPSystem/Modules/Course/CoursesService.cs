using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Course.Models;
using ERPSystem.Shared.ActivityLogs;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ERPSystem.Shared.BusinessLogic;

public class CoursesService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CoursesService> _logger;

    public CoursesService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger<CoursesService> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<PublicResponse> ListAsync(string? q)
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

            var items = await query
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Name)
                .Select(x => new CourseListItemDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsActive = x.IsActive,
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
                return response.SetError(ErrorCodes.InvalidParameters, "Course not found");

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
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime.ToString("HH:mm"),
                    EndTime = s.EndTime.ToString("HH:mm"),
                    Capacity = s.Capacity,
                    EnrolledActiveCount = sessionCounts.TryGetValue(s.Id, out var cnt) ? cnt : 0,
                    TeacherUserId = s.TeacherUserId,
                    TeacherName = s.Teacher.UserName ?? s.Teacher.Email ?? s.TeacherUserId,

                    Fee = s.Fee,
                    FeeType = s.FeeType,              
                    TotalSessions = s.TotalSessions   
                })
                .ToList();

            var dto = new CourseDetailsDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAtUtc = c.CreatedAtUtc,
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

                    Title = $"{dto.Name} - {s.DayOfWeek}"
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
                EntityId = c.Id,
                Action = "Create",
                Description = description,
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = "system"
            });


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

            // 🔥 OLD VALUES
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

            // 🔥 UPDATE COURSE
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

            // 🔥 detectăm ștergeri
            var removedSessions = c.Sessions
                .Where(s => !incomingIds.Contains(s.Id))
                .ToList();

            c.Sessions.RemoveAll(s => !incomingIds.Contains(s.Id));

            // 🔥 detectăm adăugări
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
                        Title = $"{dto.Name} - {sDto.DayOfWeek}"
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
                    existing.TotalSessions = sDto.FeeType == CourseFeeType.FixedPackage
                        ? sDto.TotalSessions
                        : null;
                }


            }

            await _db.SaveChangesAsync(); // 🔥 audit automat

            // 🔥 luăm profesori (pentru nume)
            var teacherIds = dto.Sessions
                .Select(x => x.TeacherUserId)
                .Concat(oldSessions.Values.Select(x => x.TeacherUserId))
                .Distinct()
                .ToList();

            var teachers = await _db.Users
                .Where(u => teacherIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? u.Id);

            var changes = new List<string>();

            // 🔥 schimbări simple
            if (oldName != c.Name)
                changes.Add($"Nume: '{oldName}' → '{c.Name}'");

            if (oldIsActive != c.IsActive)
                changes.Add(c.IsActive ? "Curs activat" : "Curs dezactivat");

            
            // 🔥 sesiuni adăugate
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

            // 🔥 sesiuni șterse
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

            // 🔥 sesiuni modificate
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

            // 🔥 LOG FINAL
            if (changes.Any())
            {
                _db.ActivityLog.Add(new ERPSystem.Data.Entities.ActivityLog
                {
                    EntityType = "Course",
                    EntityId = c.Id,
                    Action = "Update",
                    Description = $"Curs actualizat:\n{string.Join("\n", changes)}",
                    CreatedAtUtc = DateTime.UtcNow,
                    PerformedBy = "system"
                });

                await _db.SaveChangesAsync();
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
            var c = await _db.Courses.FirstOrDefaultAsync(x => x.Id == id);
            if (c is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Course not found");

            _db.Courses.Remove(c);
            await _db.SaveChangesAsync();

            return response.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }


    public async Task<PublicResponse> ListEnrollmentsAsync(int courseId)
    {
        var response = new PublicResponse(true);

        try
        {
            var items = await _db.CourseEnrollments.AsNoTracking()
           .Include(x => x.Student)
           .Include(x => x.Session)
           .Where(x => x.CourseId == courseId) 
           .OrderByDescending(x => x.IsActive)
           .ThenBy(x => x.Student.FullName)
           .Select(x => new EnrollmentDto(
               x.StudentId,
               x.Student.FullName,
               x.Student.Email,
               x.EnrolledAtUtc,
               x.IsActive,
               x.CourseSessionId,                         
               x.Session.DayOfWeek,
               x.Session.StartTime.ToString("HH:mm"),
               x.Session.EndTime.ToString("HH:mm")
           ))
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
                return response.SetError(ErrorCodes.InvalidParameters, "Course not found");

            var student = await _db.Students.FindAsync(studentId);
            if (student is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Student not found");

            var session = await _db.CourseSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.CourseId == courseId);

            if (session is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Session not found for this course");

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
                return response.SetError(ErrorCodes.InvalidParameters, "Student already active");

            _db.CourseEnrollments.Add(new CourseEnrollment
            {
                CourseId = courseId,
                CourseSessionId = sessionId,
                StudentId = studentId,
                EnrolledAtUtc = DateTime.UtcNow,
                IsActive = true
            });

            

            var sessionInfo = $"{session.DayOfWeek} {session.StartTime:HH:mm}";

            var description = $"Studentul {student.FullName} a fost înscris la cursul {course.Name} ({sessionInfo})";
              

            // 🔥 LOG STUDENT
            _db.ActivityLog.Add(new ERPSystem.Data.Entities.ActivityLog
            {
                EntityType = "Student",
                EntityId = studentId,
                Action =  "Enroll",
                Description = description,
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = "system"
            });

            // 🔥 LOG COURSE
            _db.ActivityLog.Add(new ERPSystem.Data.Entities.ActivityLog
            {
                EntityType = "Course",
                EntityId = courseId,
                Action =  "EnrollStudent",
                Description = description,
                CreatedAtUtc = DateTime.UtcNow,
                PerformedBy = "system"
            });

            await _db.SaveChangesAsync();

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




                var sessionInfo = $"{session.DayOfWeek} {session.StartTime:HH:mm}";

                var description = $"Studentul {student!.FullName} a fost eliminat din cursul {course!.Name} ({sessionInfo})";

                // 🔥 STUDENT LOG
                _db.ActivityLog.Add(new ERPSystem.Data.Entities.ActivityLog
                {
                    EntityType = "Student",
                    EntityId = studentId,
                    Action = "EnrollDeactivate",
                    Description = description,
                    CreatedAtUtc = DateTime.UtcNow,
                    PerformedBy = "system"
                });

                // 🔥 COURSE LOG
                _db.ActivityLog.Add(new ERPSystem.Data.Entities.ActivityLog
                {
                    EntityType = "Course",
                    EntityId = courseId,
                    Action = "StudentRemoved",
                    Description = description,
                    CreatedAtUtc = DateTime.UtcNow,
                    PerformedBy = "system"
                });

                await _db.SaveChangesAsync();
                return response.SetSuccess(true); // 🔥 FIX

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
                select new TeacherOptionDto(u.Id, u.UserName ?? u.Email ?? u.Id)
            ).ToListAsync();

            return response.SetSuccess(teachers);
        }
        catch (Exception)
        {
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }


    private static TimeOnly ParseTime(string s)
    {
        if (!TimeOnly.TryParse(s, out var t))
            throw new ArgumentException($"Invalid time: {s}. Use HH:mm");
        return t;
    }

    private static string? ValidateCreate(CreateCourseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Name is required";

        if (dto.Sessions is null || dto.Sessions.Count == 0)
            return "Course must have at least one session";

        foreach (var s in dto.Sessions)
        {
            if (string.IsNullOrWhiteSpace(s.TeacherUserId))
                return "Each session must have a teacher";
        }
        foreach (var s in dto.Sessions)
        {
            if (s.FeeType == CourseFeeType.FixedPackage)
            {
                if (!s.TotalSessions.HasValue || s.TotalSessions <= 0)
                    return "TotalSessions este obligatoriu pentru pachet fix.";
            }

            if (s.FeeType == CourseFeeType.Monthly)
            {
                if (s.TotalSessions != null)
                    return "TotalSessions nu este permis pentru abonament.";
            }

            if (s.Fee <= 0)
                return "Fee trebuie să fie mai mare decât 0.";
        }

        return null;
    }

    private static string? ValidateUpdate(UpdateCourseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return "Name is required";

        if (dto.Sessions is null || dto.Sessions.Count == 0)
            return "Course must have at least one session";

        foreach (var s in dto.Sessions)
        {
            if (string.IsNullOrWhiteSpace(s.TeacherUserId))
                return "Each session must have a teacher";
        }
        foreach (var s in dto.Sessions)
        {
            if (s.FeeType == CourseFeeType.FixedPackage)
            {
                if (!s.TotalSessions.HasValue || s.TotalSessions <= 0)
                    return "TotalSessions este obligatoriu pentru pachet fix.";
            }

            if (s.FeeType == CourseFeeType.Monthly)
            {
                if (s.TotalSessions != null)
                    return "TotalSessions nu este permis pentru abonament.";
            }

            if (s.Fee <= 0)
                return "Fee trebuie să fie mai mare decât 0.";
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

    public async Task<PublicResponse> GetAvailableStudentsAsync( int courseId, int sessionId, string? q)
    {
        var response = new PublicResponse(true);

        try
        {
            var sessionExists = await _db.CourseSessions
                .AnyAsync(s => s.Id == sessionId && s.CourseId == courseId);

            if (!sessionExists)
                return response.SetError(
                    ErrorCodes.InvalidParameters,
                    "Session not found for this course"
                );

            var enrolledIds = await _db.CourseEnrollments
                .Where(e =>
                    e.CourseSessionId == sessionId &&
                    e.IsActive)
                .Select(e => e.StudentId)
                .ToListAsync();

            var query = _db.Students.AsNoTracking()
                .Where(s => !enrolledIds.Contains(s.Id));

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
}
