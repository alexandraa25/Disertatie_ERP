using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Company.Models;
using ERPSystem.Utils.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Modules.Company
{
    public class CompanyService
    {
        private readonly ApplicationDbContext _db;
        private readonly ActivityLogService _activityLogService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CompanyService(
            ApplicationDbContext db,
            ActivityLogService activityLogService,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _activityLogService = activityLogService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<PublicResponse> GetAsync()
        {
            var response = new PublicResponse(true);

            var company = await _db.CompanySettings.FirstOrDefaultAsync();

            return response.SetSuccess(company);
        }

        public async Task<PublicResponse> SaveAsync(CompanySettingsDto dto)
        {
            var response = new PublicResponse(true);

            var company = await _db.CompanySettings.FirstOrDefaultAsync();

            bool isNew = company == null;

            if (company == null)
            {
                company = new CompanySettings
                {
                    Name = dto.Name,
                    CUI = dto.CUI,
                    RegistrationNumber = dto.RegistrationNumber,
                    Address = dto.Address,
                    IBAN = dto.IBAN,
                    Bank = dto.Bank,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    LogoPath = dto.LogoPath,
                    SignatureImage = dto.SignatureImage,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                _db.CompanySettings.Add(company);
            }
            else
            {
                company.Name = dto.Name;
                company.CUI = dto.CUI;
                company.RegistrationNumber = dto.RegistrationNumber;
                company.Address = dto.Address;
                company.IBAN = dto.IBAN;
                company.Bank = dto.Bank;
                company.Email = dto.Email;
                company.Phone = dto.Phone;
                company.LogoPath = dto.LogoPath;
                company.SignatureImage = dto.SignatureImage;
                company.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            var performedBy = _httpContextAccessor.HttpContext?.User?
               .FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
               ?? "system";

            await _activityLogService.AddAsync(
                entityType: "CompanySettings",
                entityId: company.Id.ToString(),
                action: isNew ? "Created" : "Updated",
                description: isNew ? "Datele companiei au fost create.": "Datele companiei au fost actualizate.",
                performedBy: performedBy
            );
            return response.SetSuccess();
        }
    }
}
