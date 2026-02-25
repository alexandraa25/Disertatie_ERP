namespace ERPSystem.Modules.Course.Models;

public record CourseListItemDto(
    int Id,
    string Name,
    decimal? Price,
    bool IsActive,
    DateTime CreatedAtUtc
);

public record CourseDetailsDto(
    int Id,
    string Name,
    string? Description,
    decimal? Price,
    bool IsActive,
    DateTime CreatedAtUtc,
    List<CourseSessionDto> Sessions
);

public record CreateCourseDto(
    string Name,
    string? Description,
    decimal? Price,
    List<CourseSessionUpsertDto> Sessions
);

public record UpdateCourseDto(
    string Name,
    string? Description,
    decimal? Price,
    bool IsActive,
    List<CourseSessionUpsertDto> Sessions
);

public record CourseSessionDto(
    int Id,
    int DayOfWeek,
    string StartTime,
    string EndTime,
    int? Capacity,
    int EnrolledActiveCount,
    string TeacherUserId,
    string TeacherName
);

public record CourseSessionUpsertDto(
    int? Id,
    int DayOfWeek,
    string StartTime,
    string EndTime,
    int? Capacity,
    string TeacherUserId


);
public record EnrollmentDto(
    int StudentId,
    string StudentName,
    string? StudentEmail,
    DateTime EnrolledAtUtc,
    bool IsActive,
    int SessionId,
    int DayOfWeek,
    string StartTime,
    string EndTime
);

public record EnrollStudentRequest(int StudentId, int SessionId);


public record TeacherOptionDto(string UserId, string DisplayName);
