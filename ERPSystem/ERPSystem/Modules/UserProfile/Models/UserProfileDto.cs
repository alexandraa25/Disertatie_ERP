using ERPSystem.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace ERPSystem.Modules.UserProfile.Models
{
    public class UserProfileDto
    {
        public string Id { get; set; } = default!;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }

        public bool EmailConfirmed { get; set; }

        public string? PhoneNumber { get; set; }

        public string[] Roles { get; set; } = [];

        public bool IsActive { get; set; }

        public DateTime? BirthdayDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public string? AvatarUrl { get; set; }

        public int UnreadNotifications { get; set; }

        public Guid? EmployeeId { get; set; }

        public string? JobTitle { get; set; }

        public DateTime? HireDate { get; set; }

        public decimal? Salary { get; set; }

        public string? ContractType { get; set; }

        public string? EmploymentStatus { get; set; }

        public DateTime? TerminationDate { get; set; }

        public AddressDto? Address { get; set; }

        public ContactDto? Contact { get; set; }

        public BankDto? Bank { get; set; }

        public List<DocumentDto>? Documents { get; set; }
    }

    public class UpdateUserProfileDto
    {
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? PhoneNumber { get; set; }

        public DateTime? BirthdayDate { get; set; }

        public string? AvatarUrl { get; set; }

        public string? Street { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        public string? PostalCode { get; set; }

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactPhone { get; set; }
    }

    public class AddressDto
    {
        public string? Street { get; set; }

        public string? City { get; set; }

        public string? Country { get; set; }

        public string? PostalCode { get; set; }
    }

    public class ContactDto
    {
        public string? PhoneNumber { get; set; }

        public string? EmergencyContactName { get; set; }

        public string? EmergencyContactPhone { get; set; }
    }

    public class BankDto
    {
        public string? IBAN { get; set; }

        public string? BankName { get; set; }
    }

    public class DocumentDto
    {
        public Guid Id { get; set; }

        public string FileName { get; set; } = default!;

        public string FilePath { get; set; } = default!;

        public string? DocumentType { get; set; }

        public DateTime UploadedAt { get; set; }
    }
}