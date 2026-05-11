namespace ERPSystem.Modules.Student.Models
{
    public class StudentDtos
    {
    }

    public class StudentListItemDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = default!;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class GuardianDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string RelationshipType { get; set; } = default!;
        public bool IsPrimaryContact { get; set; }
    }

    public class StudentDetailsDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = default!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public List<GuardianDto> Guardians { get; set; } = new();
    }

    public class CreateGuardianDto
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string RelationshipType { get; set; } = default!;
        public bool IsPrimaryContact { get; set; }
    }

    public class CreateStudentDto
    {
        public string FullName { get; set; } = default!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public List<CreateGuardianDto>? Guardians { get; set; }
    }

    public class UpdateStudentDto
    {
        public string FullName { get; set; } = default!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool IsActive { get; set; }
        public List<CreateGuardianDto>? Guardians { get; set; }
    }

    public class StudentCourseDetailsDto
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = default!;
        public decimal Price { get; set; }

        public int FeeType { get; set; }
        public int SessionId { get; set; }
        public string DayOfWeek { get; set; } = default!;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public string TeacherName { get; set; } = default!;

        public bool IsActive { get; set; }
        public DateTime? EndedAtUtc { get; set; }
        public int? ContractId { get; set; }
    }

    public class PagedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<T> Items { get; set; } = new();
    }

    public class StudentOptionDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = default!;
        public bool IsMinor { get; set; }
    }

    public class GuardianOptionDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = default!;
    }

    public class AvailableCourseDto
    {
        public int CourseId { get; set; }
        public int SessionId { get; set; }

        public string CourseName { get; set; } = default!;

        public int DayOfWeek { get; set; }

        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public string TeacherName { get; set; } = default!;

        public decimal Price { get; set; }

        public string FeeType { get; set; } = string.Empty;

        public int? Capacity { get; set; }
        public int Enrolled { get; set; }
    }
}