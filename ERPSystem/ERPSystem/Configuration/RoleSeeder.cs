using ERPSystem.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;

namespace ERPSystem.Configuration
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var roles = new[]
            {
                "Admin",
                "Manager",
                "Secretary",
                "Teacher",
                "Accountant",
                "Marketing",
                "Student"
            };


            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}