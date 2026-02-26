using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Authentificate;
using ERPSystem.Shared.BusinessLogic;
using ERPSystem.Utils.Settings;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ERPSystem.Modules.UserProfile;
using ERPSystem.Modules.Student;
using ERPSystem.Modules.Contracts;

namespace ERPSystem.Configuration
{
    public static class ServiceConfiguration
    {
        public static void ConfigureAllServices(this WebApplicationBuilder builder)
        {
            builder.ConfigureSettings();
            builder.ConfigureService();
            builder.ConfigureBusinessLogic();
            builder.ConfigureLogger();
            builder.ConfigureCors();
            builder.ConfigureContext();
        }

        public static void ConfigureService(this WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<AuthentificationService>();
        }

        public static void ConfigureBusinessLogic(this WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<EmailBusinessLogic>();
            builder.Services.AddScoped<UserProfileBusinessLogic>();
            builder.Services.AddScoped<StudentsService>();
            builder.Services.AddScoped<CoursesService>();
            builder.Services.AddScoped<ContractsService>();
        }

        public static void ConfigureSettings(this WebApplicationBuilder builder)
        {
            builder.Services.Configure<EmailConnectionSettings>(builder.Configuration.GetSection("EmailConnectionSettings"));
            builder.Services.Configure<ERPSystemSettings>(builder.Configuration.GetSection("ERPSystemSettings"));
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
        }

        public static void ConfigureContext(this WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Identity + API Endpoints
            builder.Services.AddIdentityApiEndpoints<ApplicationUser>().AddEntityFrameworkStores<ApplicationDbContext>();
        }

        public static void ConfigureLogger(this WebApplicationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            builder.Host.UseSerilog();
        }

        public static void ConfigureCors(this WebApplicationBuilder builder)
        {
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("frontend", policy =>
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials());
            });
        }
    }
}
