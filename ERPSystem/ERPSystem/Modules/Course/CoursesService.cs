using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Course.Models;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Shared.BusinessLogic;

public class CoursesService
{
    #region Constructor

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CoursesService> _logger;

    public CoursesService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger<CoursesService> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    #endregion

    #region Courses - List

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
                .Select(x => new CourseListItemDto(x.Id, x.Name, x.Price, x.IsActive, x.CreatedAtUtc))
                .ToListAsync();

            return response.SetSuccess(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    #endregion

    #region Courses - Details

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
                .Select(s => new CourseSessionDto(
                    s.Id,
                    s.DayOfWeek,
                    s.StartTime.ToString("HH:mm"),
                    s.EndTime.ToString("HH:mm"),
                    s.Capacity,
                    sessionCounts.TryGetValue(s.Id, out var cnt) ? cnt : 0,
                    s.TeacherUserId,
                    s.Teacher.UserName ?? s.Teacher.Email ?? s.TeacherUserId
                ))
                .ToList();

            var dto = new CourseDetailsDto(
                c.Id,
                c.Name,
                c.Description,
                c.Price,
                c.IsActive,
                c.CreatedAtUtc,
                sessions
            );

            return response.SetSuccess(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    #endregion

    #region Courses - Create

    public async Task<PublicResponse> CreateAsync(CreateCourseDto dto)
    {
        var response = new PublicResponse(true);

        try
        {
            var validation = ValidateCreate(dto);
            if (validation is not null)
                return response.SetError(ErrorCodes.InvalidParameters, validation);

            // 🔥 VALIDARE SUPRAPUNERE PROFESOR
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
                Price = dto.Price,
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
                    TeacherUserId = s.TeacherUserId
                });
            }

            _db.Courses.Add(c);
            await _db.SaveChangesAsync();

            return response.SetCreated(new { id = c.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    #endregion

    #region Courses - Update

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
                return response.SetError(ErrorCodes.InvalidParameters, "Course not found");

            c.Name = dto.Name.Trim();
            c.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            c.Price = dto.Price;
            c.IsActive = dto.IsActive;
            if (!dto.IsActive)
            {
                foreach (var session in c.Sessions)
                {
                    session.IsActive = false;
                }
            }
            // c.TeacherUserId = dto.TeacherUserId;
            c.UpdatedAtUtc = DateTime.UtcNow;

            // upsert sessions
            var incomingIds = dto.Sessions
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .ToHashSet();

            // șterge sesiunile eliminate
            c.Sessions.RemoveAll(s => !incomingIds.Contains(s.Id));

            foreach (var sDto in dto.Sessions)
            {
                if (sDto.Id is null)
                {
                    c.Sessions.Add(new CourseSession
                    {
                        DayOfWeek = sDto.DayOfWeek,
                        StartTime = ParseTime(sDto.StartTime),
                        EndTime = ParseTime(sDto.EndTime),
                        Capacity = sDto.Capacity,
                        TeacherUserId = sDto.TeacherUserId   // 🔥 IMPORTANT
                    });
                }
                else
                {
                    var existing = c.Sessions.FirstOrDefault(x => x.Id == sDto.Id.Value);
                    if (existing is null) continue;

                    existing.DayOfWeek = sDto.DayOfWeek;
                    existing.StartTime = ParseTime(sDto.StartTime);
                    existing.EndTime = ParseTime(sDto.EndTime);
                    existing.Capacity = sDto.Capacity;
                    existing.TeacherUserId = sDto.TeacherUserId;   // 🔥 IMPORTANT
                }
            }

            await _db.SaveChangesAsync();

            return response.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    #endregion

    #region Courses - Delete

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

    #endregion

    #region Courses - Enrollments

    public async Task<PublicResponse> ListEnrollmentsAsync(int courseId)
    {
        var response = new PublicResponse(true);

        try
        {
            var items = await _db.CourseEnrollments.AsNoTracking()
           .Include(x => x.Student)
           .Include(x => x.Session)
           .Where(x => x.CourseId == courseId) // tu ai păstrat CourseId
           .OrderByDescending(x => x.IsActive)
           .ThenBy(x => x.Student.FullName)
           .Select(x => new EnrollmentDto(
               x.StudentId,
               x.Student.FullName,
               x.Student.Email,
               x.EnrolledAtUtc,
               x.IsActive,
               x.CourseSessionId,                         // SessionId (int)
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
            var courseExists = await _db.Courses.AnyAsync(x => x.Id == courseId);
            if (!courseExists)
                return response.SetError(ErrorCodes.InvalidParameters, "Course not found");

            var studentExists = await _db.Students.AnyAsync(x => x.Id == studentId);
            if (!studentExists)
                return response.SetError(ErrorCodes.InvalidParameters, "Student not found");

            var session = await _db.CourseSessions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.CourseId == courseId);

            if (session is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Session not found for this course");

            // verifica capacity pe sesiune
            if (session.Capacity.HasValue)
            {
                var activeCount = await _db.CourseEnrollments
                    .CountAsync(e => e.CourseSessionId == sessionId && e.IsActive);

                if (activeCount >= session.Capacity.Value)
                    return response.SetError(ErrorCodes.InvalidParameters, "Sesiunea a atins limita de cursanți.");
            }

            // upsert enrollment pe sesiune + student
            var existing = await _db.CourseEnrollments
                .FirstOrDefaultAsync(x => x.CourseSessionId == sessionId && x.StudentId == studentId);

            if (existing is null)
            {
                _db.CourseEnrollments.Add(new CourseEnrollment
                {
                    CourseId = courseId,
                    CourseSessionId = sessionId,
                    StudentId = studentId,
                    EnrolledAtUtc = DateTime.UtcNow,
                    IsActive = true
                });
            }
            else
            {
                existing.IsActive = true;
                existing.EnrolledAtUtc = DateTime.UtcNow;
            }

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

            if (existing is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Enrollment not found");

            existing.IsActive = isActive;
            await _db.SaveChangesAsync();

            return response.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetEnrollmentActiveAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    #endregion

    #region Courses - Teachers

    public async Task<PublicResponse> GetTeachersAsync()
    {
        var response = new PublicResponse(true);

        try
        {
            var roleId = await _db.Roles
                .Where(r => r.Name == "PROFESOR")
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


    #endregion

    #region Helpers

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

        return null;
    }

    #endregion

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

    private async Task<bool> HasTeacherOverlapInDatabase(
    List<CourseSessionUpsertDto> sessions,
    int? currentCourseId = null)
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

            // dacă e update, excludem sesiunile din cursul curent
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

    public async Task<PublicResponse> GetAvailableStudentsAsync(
    int courseId,
    int sessionId,
    string? q)
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
