using ERPSystem.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Modules.UserProfile.Models
{

    public record UserProfileDto(
        string? FirstName,
        string? LastName,
        string? FullName,
        string? Username,
        string? Email,
        bool EmailConfirmed,
        string? PhoneNumber,
        string[] Roles,
        bool IsActive,
        DateTime? BirthdayDate,
        DateTime CreatedAt,
        DateTime? LastLoginAt,
        string? AvatarUrl,
        int UnreadNotifications,

        Guid? EmployeeId,
        string? JobTitle,
        DateTime? HireDate,
        decimal? Salary,
        string? ContractType,
        string? EmploymentStatus,

        AddressDto? Address,
        ContactDto? Contact,
        BankDto? Bank,
        List<DocumentDto>? Documents
    );

    public record UpdateUserProfileDto(
        string? FirstName,
        string? LastName,
        string? PhoneNumber,
        DateTime? BirthdayDate,
        string? AvatarUrl,

        string? Street,
        string? City,
        string? Country,
        string? PostalCode,

        string? EmergencyContactName,
        string? EmergencyContactPhone
    );


    public record AddressDto(
        string? Street,
        string? City,
        string? Country,
        string? PostalCode
    );

    public record ContactDto(
        string? PhoneNumber,
        string? EmergencyContactName,
        string? EmergencyContactPhone
    );

    public record BankDto(
        string? IBAN,
        string? BankName
    );

    public record DocumentDto(
        Guid Id,
        string FileName,
        string FilePath,
        string? DocumentType,
        DateTime UploadedAt
    );

    
}