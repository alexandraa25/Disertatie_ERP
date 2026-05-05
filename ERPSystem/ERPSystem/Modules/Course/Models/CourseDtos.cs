using ERPSystem.Data.Entities;

namespace ERPSystem.Modules.Course.Models;

public class CourseListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
}

public class CourseDetailsDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

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
    public string Title { get; set; }

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

    public bool IsActive { get; set; }
}


public class EnrollmentDto
{
    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public string? StudentEmail { get; set; }

    public DateTime EnrolledAtUtc { get; set; }

    public bool IsActive { get; set; }

    public int SessionId { get; set; }

    public int DayOfWeek { get; set; }

    public string StartTime { get; set; } = string.Empty;

    public string EndTime { get; set; } = string.Empty;

    public bool FeedbackSent { get; set; }

    public DateTime? FeedbackSentAt { get; set; }

    public DateTime? UnenrolledAtUtc { get; set; }
}

public class EnrollStudentRequest
{
    public int StudentId { get; set; }

    public int SessionId { get; set; }
}


public class TeacherOptionDto
{
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
