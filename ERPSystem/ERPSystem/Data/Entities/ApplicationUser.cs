using Microsoft.AspNetCore.Identity;

namespace ERPSystem.Data.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
  
    public ApplicationUser()
    {
    }

    public ApplicationUser(string userName, string email,string firstName, string lastName)
    {
        UserName = userName;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        FullName = $"{firstName} {lastName}";
       
    }
}
