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

    public record PagedResult<T>(
        int Page,
        int PageSize,
        int Total,
        List<T> Items
    );

    public record StudentOptionDto(
    int Id,
    string FullName
);
}
