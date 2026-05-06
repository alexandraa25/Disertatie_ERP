using ClosedXML.Excel;
using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Student.Models;
using ERPSystem.Modules.Students.Models;
using ERPSystem.Shared.Notifications;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Enums;
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

    public async Task<PublicResponse> GetStudentsAsync(  string? q,  int page,   int pageSize,  string? sortBy,  string? sortDir,  int? recentDays,  bool? onlyRecent, int? sessionId, string? statusFilter, string? deleteFilter)
    {
        var response = new PublicResponse(true);

        var query = BuildStudentsQuery(q, sortBy, sortDir, onlyRecent, recentDays, sessionId, statusFilter, deleteFilter);

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
                 s.CreatedAtUtc,
                 s.IsDeleted
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
                   s.IsDeleted,
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
            AddStudentLog(  s.Id, "Create", $"Cursantul {s.FullName} a fost creat.");

            await _db.SaveChangesAsync();

            await NotifyStudentRolesAsync( s, "Cursant creat", $"Cursantul {s.FullName} a fost adăugat în sistem.", "Success" );
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

            if (s.IsDeleted)
                return response.SetError(ErrorCodes.InvalidParameters, "Nu poți modifica un cursant șters.");

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
            AddStudentLog( s.Id, "Update", $"Datele cursantului {s.FullName} au fost actualizate.");

            await _db.SaveChangesAsync();

            await NotifyStudentRolesAsync(s, "Date cursant actualizate", $"Datele cursantului {s.FullName} au fost actualizate.","Info" );

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

                AddStudentLog( s.Id, "UpdateGuardians", description.Trim());

                await _db.SaveChangesAsync();

                await NotifyStudentRolesAsync( s,"Tutori cursant actualizați", $"Tutorii cursantului {s.FullName} au fost actualizați.",  "Warning" );
            }

            return response.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateAsync failed");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> ToggleStatusAsync(int id)
    {
        var response = new PublicResponse(true);

        try
        {
            var s = await _db.Students.FirstOrDefaultAsync(x => x.Id == id);

            if (s is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Student not found");

            if (s.IsDeleted)
                return response.SetError(ErrorCodes.InvalidParameters, "Nu poți modifica statusul unui cursant șters.");

            s.IsActive = !s.IsActive;
            s.UpdatedAtUtc = DateTime.UtcNow;

            var action = s.IsActive ? "Activate" : "Deactivate";

            var description = s.IsActive
                ? $"Cursantul {s.FullName} a fost activat."
                : $"Cursantul {s.FullName} a fost dezactivat.";

            AddStudentLog(s.Id, action, description);

            await _db.SaveChangesAsync();

            await NotifyStudentRolesAsync(  s, s.IsActive ? "Cursant activat" : "Cursant dezactivat", description, s.IsActive ? "Success" : "Warning" );

            return response.SetSuccess(new
            {
                s.Id,
                s.IsActive,
                s.IsDeleted
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ToggleStatusAsync failed");
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

            if (s.IsDeleted)
                return response.SetError(ErrorCodes.InvalidParameters, "Cursantul este deja șters.");

            if (s.IsActive)
                return response.SetError(ErrorCodes.InvalidParameters, "Dezactivează cursantul înainte de ștergere.");

            if (s.IsActive)
                return response.SetError(ErrorCodes.InvalidParameters,
                    "Dezactivează cursantul înainte de ștergere.");

            var hasActiveEnrollments = await _db.CourseEnrollments
                .AnyAsync(e => e.StudentId == id && e.IsActive);

            if (hasActiveEnrollments)
                return response.SetError(ErrorCodes.InvalidParameters,
                    "Nu poți șterge cursantul. Are înscrieri active.");

            var hasActiveContracts = await _db.StudentContracts
                .AnyAsync(c =>
                    c.Parties.Any(p => p.StudentId == id) &&
                    c.Status == ContractStatus.Active);

            if (hasActiveContracts)
                return response.SetError(ErrorCodes.InvalidParameters,
                    "Nu poți șterge cursantul. Are contracte active.");

            s.IsDeleted = true;
            s.DeletedAtUtc = DateTime.UtcNow;
            s.UpdatedAtUtc = DateTime.UtcNow;

            var description = $"Cursantul {s.FullName} a fost șters/arhivat.";

            AddStudentLog(s.Id, "Delete", description);

            await _db.SaveChangesAsync();

            await NotifyStudentRolesAsync(  s, "Cursant arhivat",  description,  "Warning" );

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

        try
        {
            var s = await _db.Students.FirstOrDefaultAsync(x => x.Id == id);

            if (s is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Student not found");

            if (!s.IsDeleted)
                return response.SetError(ErrorCodes.InvalidParameters, "Cursantul nu este șters.");

            s.IsDeleted = false;
            s.DeletedAtUtc = null;
            s.IsActive = false;
            s.UpdatedAtUtc = DateTime.UtcNow;

            var description = $"Cursantul {s.FullName} a fost restaurat.";

            AddStudentLog(s.Id, "Restore", description);

            await _db.SaveChangesAsync();

            await NotifyStudentRolesAsync( s, "Cursant restaurat", description, "Success");

            return response.SetSuccess(new
            {
                s.Id,
                s.IsActive,
                s.IsDeleted
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RestoreAsync failed");
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

    public async Task<List<AvailableCourseDto>> GetAvailableCoursesForStudentAsync( int studentId, string? q)
    {
        var query = _db.CourseSessions
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.Teacher)
            .Where(x =>
                x.IsActive &&
                x.Course.IsActive &&
                !x.Enrollments.Any(e =>
                    e.StudentId == studentId &&
                    e.IsActive));

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(x =>
                x.Course.Name.Contains(q));
        }

        return await query
            .OrderBy(x => x.Course.Name)
            .ThenBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
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

                Enrolled = x.Enrollments.Count(e => e.IsActive)
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

    private IQueryable<Data.Entities.Student> BuildStudentsQuery( string? q, string? sortBy, string? sortDir, bool? onlyRecent,int? recentDays, int? sessionId, string? statusFilter, string? deleteFilter)
    {
        var query = _db.Students
            .AsNoTracking()
            .AsQueryable();

        query = ApplyStudentFilters(
            query,
            q,
            onlyRecent,
            recentDays,
            sessionId,
            statusFilter,
            deleteFilter
        );

        query = ApplyStudentSorting(query, sortBy, sortDir);

        return query;
    }
    public async Task<byte[]> ExportStudentsExcel( string? q, string? sortBy, string? sortDir, bool? onlyRecent,  int? recentDays, int? sessionId,  string? statusFilter, string? deleteFilter)
    {
        var query = _db.Students
            .AsNoTracking()
            .Include(x => x.StudentGuardians)
                .ThenInclude(x => x.Guardian)
            .Include(x => x.Enrollments)
                .ThenInclude(x => x.Course)
            .Include(x => x.Enrollments)
                .ThenInclude(x => x.Session)
            .Include(x => x.Enrollments)
                .ThenInclude(x => x.Contract)
            .AsQueryable();

        query = ApplyStudentFilters(  query, q, onlyRecent, recentDays, sessionId, statusFilter, deleteFilter );

        query = ApplyStudentSorting(query, sortBy, sortDir);

        var students = await query.ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Cursanți");

        var headers = new[]
        {
        "ID",
        "Nume complet",
        "Prenume",
        "Nume",
        "Email",
        "Telefon",
        "Adresă",
        "Data nașterii",
        "Vârstă",
        "Minor",
        "Status",
        "Tutori",
        "Contact principal",
        "Cursuri active",
        "Cursuri inactive",
        "Contracte asociate",
        "Data creare",
        "Ultima actualizare",
        "Data ștergerii"
    };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        for (int i = 0; i < students.Count; i++)
        {
            var s = students[i];
            var row = i + 2;

            var guardians = s.StudentGuardians.Any()
                ? string.Join(" | ", s.StudentGuardians.Select(g =>
                    $"{g.Guardian.FirstName} {g.Guardian.LastName} ({g.RelationshipType}) - {g.Guardian.Phone}"))
                : "";

            var primaryGuardian = s.StudentGuardians
                .Where(g => g.IsPrimaryContact)
                .Select(g => $"{g.Guardian.FirstName} {g.Guardian.LastName} - {g.Guardian.Phone}")
                .FirstOrDefault() ?? "";

            var activeCourses = s.Enrollments
                .Where(e => e.IsActive)
                .Select(e => $"{e.Course.Name} / {e.Session.Title}")
                .Distinct()
                .ToList();

            var inactiveCourses = s.Enrollments
                .Where(e => !e.IsActive)
                .Select(e => $"{e.Course.Name} / {e.Session.Title}")
                .Distinct()
                .ToList();

            var contracts = s.Enrollments
                .Where(e => e.Contract != null)
                .Select(e => $"{e.Contract.ContractNumber} ({e.Contract.Status})")
                .Distinct()
                .ToList();

            ws.Cell(row, 1).Value = s.Id;
            ws.Cell(row, 2).Value = s.FullName;
            ws.Cell(row, 3).Value = s.FirstName ?? "";
            ws.Cell(row, 4).Value = s.LastName ?? "";
            ws.Cell(row, 5).Value = s.Email ?? "";
            ws.Cell(row, 6).Value = s.Phone ?? "";
            ws.Cell(row, 7).Value = s.Address ?? "";
            ws.Cell(row, 8).Value = s.DateOfBirth;
            ws.Cell(row, 9).Value = s.Age;
            ws.Cell(row, 10).Value = s.IsMinor ? "Da" : "Nu";
            ws.Cell(row, 11).Value = s.IsDeleted
                ? "Șters"
                : s.IsActive
                    ? "Activ"
                    : "Inactiv";
            ws.Cell(row, 12).Value = guardians;
            ws.Cell(row, 13).Value = primaryGuardian;
            ws.Cell(row, 14).Value = string.Join(" | ", activeCourses);
            ws.Cell(row, 15).Value = string.Join(" | ", inactiveCourses);
            ws.Cell(row, 16).Value = string.Join(" | ", contracts);
            ws.Cell(row, 17).Value = s.CreatedAtUtc;
            ws.Cell(row, 18).Value = s.UpdatedAtUtc;
            ws.Cell(row, 19).Value = s.DeletedAtUtc;
        }

        FormatStudentsSheet(ws);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
    private static void FormatStudentsSheet(IXLWorksheet ws)
    {
        var usedRange = ws.RangeUsed();

        if (usedRange == null)
            return;

        usedRange.SetAutoFilter();

        var header = ws.Row(1);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.Columns().AdjustToContents();

        foreach (var column in ws.Columns())
        {
            if (column.Width > 45)
                column.Width = 45;
        }

        ws.Column(8).Style.DateFormat.Format = "dd.MM.yyyy";
        ws.Column(17).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
        ws.Column(18).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
        ws.Column(19).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";

        ws.SheetView.FreezeRows(1);
    }

    private IQueryable<Data.Entities.Student> ApplyStudentFilters(IQueryable<Data.Entities.Student> query,string? q, bool? onlyRecent, int? recentDays, int? sessionId, string? statusFilter, string? deleteFilter)
    {
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();

            query = query.Where(s =>
                s.FullName.Contains(q) ||
                (s.Email != null && s.Email.Contains(q)) ||
                (s.Phone != null && s.Phone.Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            switch (statusFilter.ToLower())
            {
                case "active":
                    query = query.Where(x => x.IsActive && !x.IsDeleted);
                    break;

                case "inactive":
                    query = query.Where(x => !x.IsActive && !x.IsDeleted);
                    break;
            }
        }

        switch ((deleteFilter ?? "notDeleted").ToLower())
        {
            case "deleted":
                query = query.Where(x => x.IsDeleted);
                break;

            case "notdeleted":
                query = query.Where(x => !x.IsDeleted);
                break;
        }

        if (onlyRecent == true)
        {
            var days = Math.Clamp(recentDays ?? 30, 1, 3650);
            var from = DateTime.UtcNow.AddDays(-days);
            query = query.Where(s => s.CreatedAtUtc >= from);
        }

        if (sessionId.HasValue)
        {
            query = query.Where(s => s.Enrollments.Any(e =>
                e.CourseSessionId == sessionId.Value &&
                e.IsActive));
        }

        return query;
    }

    private static IQueryable<Data.Entities.Student> ApplyStudentSorting( IQueryable<Data.Entities.Student> query, string? sortBy, string? sortDir)
    {
        var dirDesc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy ?? "createdAt").ToLower() switch
        {
            "fullname" or "name" => dirDesc
                ? query.OrderByDescending(s => s.FullName)
                : query.OrderBy(s => s.FullName),

            "createdat" or "date" => dirDesc
                ? query.OrderByDescending(s => s.CreatedAtUtc)
                : query.OrderBy(s => s.CreatedAtUtc),

            _ => query.OrderByDescending(s => s.CreatedAtUtc)
        };
    }

    private void AddStudentLog(int studentId, string action, string description)
    {
        _db.ActivityLog.Add(new ActivityLog
        {
            EntityType = "Student",
            EntityId = studentId.ToString(),
            Action = action,
            Description = description,
            CreatedAtUtc = DateTime.UtcNow,
            PerformedBy = "system"
        });
    }

    private Task NotifyStudentRolesAsync( Data.Entities.Student student, string title, string message, string type)
    {
        return _notificationService.CreateNotificationForRolesAsync(
            roleNames: new[] { "Admin", "Secretary" },
            eventType: NotificationEvents.StudentActivity,
            title: title,
            message: message,
            type: type,
            link: $"/students/{student.Id}",
            entityType: "Student",
            entityId: student.Id.ToString()
        );
    }
}
