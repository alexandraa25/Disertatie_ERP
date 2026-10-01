using System.Text.Json;
using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Data;

public static class DatabaseInitializer
{
    public static readonly string[] Roles =
        ["Admin", "Manager", "Secretary", "Teacher", "HR", "Accountant", "Marketing"];

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Set BootstrapAdmin__Email and BootstrapAdmin__Password before initialization.");

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var templates = LoadTemplates();

        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var admin = await users.FindByEmailAsync(email);
        if (admin is not null && !await users.IsInRoleAsync(admin, "Admin"))
            throw new InvalidOperationException("The bootstrap email already belongs to a non-administrator. No account was promoted.");

        foreach (var role in Roles)
            if (!await roles.RoleExistsAsync(role))
                EnsureSuccess(await roles.CreateAsync(new IdentityRole(role)));

        foreach (var template in templates.EmailTemplates)
            if (!await db.EmailTemplates.AnyAsync(x => x.TemplateCode == template.TemplateCode))
                db.EmailTemplates.Add(template);

        foreach (var template in templates.ContractTemplates)
            if (!await db.ContractTemplates.AnyAsync(x => x.Name == template.Name))
            {
                template.CreatedAtUtc = DateTime.UtcNow;
                db.ContractTemplates.Add(template);
            }
        await db.SaveChangesAsync();

        if (admin is null)
        {
            admin = new ApplicationUser(email, email, "Administrator", "ERP")
            {
                EmailConfirmed = true,
                MustChangePassword = true
            };
            EnsureSuccess(await users.CreateAsync(admin, password));
            EnsureSuccess(await users.AddToRoleAsync(admin, "Admin"));
        }

        await transaction.CommitAsync();
        Console.WriteLine("Database initialized. Existing accounts and templates were preserved. Configure company details in the application before creating contracts.");
    }

    public static TemplateSeed LoadTemplates()
    {
        using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream("ERPSystem.SeedTemplates")
            ?? throw new InvalidOperationException("Seed templates are missing.");
        return JsonSerializer.Deserialize<TemplateSeed>(stream)
            ?? throw new InvalidOperationException("Seed templates are invalid.");
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }

    public sealed class TemplateSeed
    {
        public List<EmailTemplate> EmailTemplates { get; set; } = [];
        public List<ContractTemplate> ContractTemplates { get; set; } = [];
    }
}
