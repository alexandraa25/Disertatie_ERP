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

        public CompanyService(ApplicationDbContext db)
        {
            _db = db;
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
            return response.SetSuccess();
        }
    }
}
