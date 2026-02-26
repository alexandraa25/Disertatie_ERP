using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Student.Models;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;


namespace ERPSystem.Modules.Student;

public class StudentsService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<StudentsService> _logger;

    public StudentsService(ApplicationDbContext db, ILogger<StudentsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PublicResponse> GetStudentsAsync(
        string? q,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDir,
        int? recentDays,
        bool? onlyRecent)
    {
        var response = new PublicResponse(true);

        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 5, 100);

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
                var days = Math.Clamp(recentDays.GetValueOrDefault(30), 1, 3650);
                var from = DateTime.UtcNow.AddDays(-days);
                query = query.Where(s => s.CreatedAtUtc >= from);
            }

            var dirDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            sortBy = (sortBy ?? "createdAt").Trim().ToLowerInvariant();

            query = sortBy switch
            {
                "fullname" => dirDesc ? query.OrderByDescending(s => s.FullName) : query.OrderBy(s => s.FullName),
                "createdat" or "createdatutc" or "createdatdate" or "createdat" => dirDesc
                    ? query.OrderByDescending(s => s.CreatedAtUtc)
                    : query.OrderBy(s => s.CreatedAtUtc),
                _ => query.OrderByDescending(s => s.CreatedAtUtc)
            };

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

            var result = new PagedResult<StudentListItemDto>(page, pageSize, total, items);

            return response.SetSuccess(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetStudentsAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
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

            // 🔥 REGULA MINOR
            if (s.IsMinor && (dto.Guardians == null || dto.Guardians.Count == 0))
                return response.SetError(ErrorCodes.InvalidParameters,
                    "Student minor trebuie să aibă cel puțin un părinte.");

            // 🔥 Validare un singur primary
            if (dto.Guardians != null && dto.Guardians.Count(g => g.IsPrimaryContact) > 1)
                return response.SetError(ErrorCodes.InvalidParameters,
                    "Poate exista un singur contact principal.");

            if (dto.Guardians != null)
            {
                foreach (var g in dto.Guardians)
                {
                    var email = g.Email.Trim();

                    // 🔥 REUTILIZARE GUARDIAN EXISTENT
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

            s.FullName = dto.FullName.Trim();
            s.FirstName = string.IsNullOrWhiteSpace(dto.FirstName) ? null : dto.FirstName.Trim();
            s.LastName = string.IsNullOrWhiteSpace(dto.LastName) ? null : dto.LastName.Trim();
            s.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            s.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
            s.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
            s.DateOfBirth = dto.DateOfBirth;
            s.IsActive = dto.IsActive;
            s.UpdatedAtUtc = DateTime.UtcNow;

            // 🔥 VALIDARE BUSINESS
            if (s.IsMinor && (dto.Guardians == null || dto.Guardians.Count == 0))
                return response.SetError(ErrorCodes.InvalidParameters,
                    "Student minor trebuie să aibă cel puțin un părinte.");

            if (dto.Guardians != null)
            {
                // 🔥 Validare un singur primary
                if (dto.Guardians.Count(g => g.IsPrimaryContact) > 1)
                    return response.SetError(ErrorCodes.InvalidParameters,
                        "Poate exista un singur contact principal.");

                // 🔥 Ștergem relațiile vechi
                _db.StudentGuardians.RemoveRange(s.StudentGuardians);

                // 🔥 Re-adăugăm lista nouă
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

            await _db.SaveChangesAsync();

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
            var courses = await _db.CourseEnrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId && e.IsActive)
                .Select(e => new StudentCourseDetailsDto
                {
                    CourseId = e.CourseId,
                    CourseName = e.Course.Name,
                    Price = e.Course.Price ?? 0m,

                    SessionId = e.CourseSessionId,
                    DayOfWeek = e.Session.DayOfWeek.ToString(),
                    StartTime = e.Session.StartTime,
                    EndTime = e.Session.EndTime,

                    TeacherName = e.Session.Teacher.FirstName + " " + e.Session.Teacher.LastName
                })
                .ToListAsync();

            var total = courses.Sum(c => c.Price);

            return response.SetSuccess(new
            {
                items = courses,
                totalAmount = total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetStudentCoursesAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
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

}
