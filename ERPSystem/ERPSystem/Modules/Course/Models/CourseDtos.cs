using ERPSystem.Data.Entities;

namespace ERPSystem.Modules.Course.Models;

public class CourseListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CourseDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<CourseSessionDto> Sessions { get; set; } = new();
}

public class CreateCourseDto
{
    public string Name { get; set; }
    public string? Description { get; set; }

    public List<CourseSessionUpsertDto> Sessions { get; set; } = new();
}

public class UpdateCourseDto
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public List<CourseSessionUpsertDto> Sessions { get; set; } = new();
}

public class CourseSessionUpsertDto
{
    public int? Id { get; set; }

    public int DayOfWeek { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }

    public int? Capacity { get; set; }
    public string TeacherUserId { get; set; }

    // 🔥 pricing
    public CourseFeeType FeeType { get; set; }
    public decimal Fee { get; set; }

    public int? TotalSessions { get; set; }
}
public class CourseSessionDto
{
    public int Id { get; set; }

    public int DayOfWeek { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }

    public int? Capacity { get; set; }
    public int EnrolledActiveCount { get; set; }

    public string TeacherUserId { get; set; }
    public string TeacherName { get; set; }

    // 🔥 IMPORTANT pt UI
    public CourseFeeType FeeType { get; set; }
    public decimal Fee { get; set; }
    public int? TotalSessions { get; set; }
}


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
