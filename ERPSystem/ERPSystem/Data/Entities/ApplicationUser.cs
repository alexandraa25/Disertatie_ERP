using Microsoft.AspNetCore.Identity;

namespace ERPSystem.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public bool IsActive { get; set; } = true;

    public bool MustChangePassword { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }

    //public DateTime? HireDate { get; set; }

    public DateTime? BirthdayDate { get; set; }

    public string? AvatarUrl { get; set; }

    public Employee? Employee { get; set; }

    public ApplicationUser()
    {
    }

    public ApplicationUser(string userName, string email, string firstName, string lastName)
    {
        UserName = userName;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        FullName = $"{firstName} {lastName}";
        IsActive = true;
        MustChangePassword = true;
        CreatedAt = DateTime.UtcNow;
    }
}