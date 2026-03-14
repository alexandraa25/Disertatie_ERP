using ERPSystem.Extensions;
using ERPSystem.Modules.Company;
using ERPSystem.Modules.Company.Models;
using ERPSystem.Modules.UserProfile;
using Route = ERPSystem.Utils.Constants.General.Route.Company;

namespace ERPSystem.Modules.Admin
{
    public class CompanyEndpoints
    {
        public static void Map(RouteGroupBuilder group)
        {

            group.MapGet(Route.COMPANY_GET,
            async (CompanyService service)
                => await service.GetAsync())
        .WithDefaultApiSettings("GetCompanySettings", "Get Company Settings", "GET_COMPANY_SETTINGS", false);


            group.MapPost(Route.COMPANY_SAVE,
                async (CompanySettingsDto dto, CompanyService service)
                    => await service.SaveAsync(dto))
            .WithDefaultApiSettings("SaveCompanySettings", "Create or Update Company Settings", "SAVE_COMPANY_SETTINGS", false);
        }
    }
}