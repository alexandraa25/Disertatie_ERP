using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Admin.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Modules.Admin
{
    public class AdminService
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminService(
            ApplicationDbContext applicationDbContex,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _applicationDbContext = applicationDbContex;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IResult> GetDashboardAsync()
        {
            var users = await _userManager.Users
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var userDtos = new List<CompanyUserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userDtos.Add(new CompanyUserDto
                {
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToList()
                });
            }

            var dashboard = new AdminDashboardDto
            {
                TotalUsers = userDtos.Count,
                ActiveUsers = userDtos.Count(x => x.IsActive),
                InactiveUsers = userDtos.Count(x => !x.IsActive),
                AdminUsers = userDtos.Count(x => x.Roles.Contains("Admin")),
                LatestUsers = userDtos.Take(3).ToList(),
                Users = userDtos
            };

            return Results.Ok(dashboard);
        }

        public async Task<IResult> GetEmployeesWithoutUserAsync()
        {
            var employees = await _applicationDbContext.Employees
                  .Where(e => e.UserId == null)
                  .Select(e => new
                  {
                      e.Id,
                      e.FirstName,
                      e.LastName,
                      e.Email,
                      e.JobTitle,
                      PhoneNumber = e.Contact != null ? e.Contact.PhoneNumber : null
                  })
                  .ToListAsync();

            return Results.Ok(employees);
        }
    }
}
