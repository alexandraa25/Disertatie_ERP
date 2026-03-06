namespace ERPSystem.Modules.Student.Models
{
    public class StudentDtos
    {
    }
    public record StudentListItemDto(
    int Id,
    string FullName,
    string? Email,
    string? Phone,
    bool IsActive,
     DateTime CreatedAtUtc
);

    // 🔹 GUARDIAN DTO
    public record GuardianDto(
        int Id,
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        string RelationshipType,
        bool IsPrimaryContact
    );

    public record StudentDetailsDto(
         int Id,
        string FullName,
        string? FirstName,
        string? LastName,
        string? Email,
        string? Phone,
        string? Address,
        DateTime? DateOfBirth,
        bool IsActive,
        List<GuardianDto> Guardians
    );

    public record CreateGuardianDto(
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        string RelationshipType,
        bool IsPrimaryContact
    );
    public record CreateStudentDto(
        string FullName,
        string? FirstName,
        string? LastName,
        string? Email,
        string? Phone,
        string? Address,
        DateTime? DateOfBirth,
        List<CreateGuardianDto>? Guardians
    );

    public record UpdateStudentDto(
      string FullName,
      string? FirstName,
      string? LastName,
      string? Email,
      string? Phone,
      string? Address,
      DateTime? DateOfBirth,
      bool IsActive,
      List<CreateGuardianDto>? Guardians
  );

    public class StudentCourseDetailsDto
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = default!;
        public decimal Price { get; set; }

        public int SessionId { get; set; }
        public string DayOfWeek { get; set; } = default!;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public string TeacherName { get; set; } = default!;
    }

    public record PagedResult<T>(
        int Page,
        int PageSize,
        int Total,
        List<T> Items
    );

    public record StudentOptionDto(
    int Id,
    string FullName,
    bool IsMinor
);

    public record GuardianOptionDto(
    int Id,
    string FullName
);

    public class AvailableCourseDto
    {
        public int CourseId { get; set; }
        public int SessionId { get; set; }

        public string CourseName { get; set; }

        public int DayOfWeek { get; set; }

        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public string TeacherName { get; set; }

        public decimal Price { get; set; }

        public int? Capacity { get; set; }
        public int Enrolled { get; set; }
    }
}
