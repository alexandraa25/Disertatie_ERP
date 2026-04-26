
using ERPSystem.Data.Entities;
using ERPSystem.Extensions;

using ERPSystem.Modules.Admin;
using ERPSystem.Modules.Employees;

using ERPSystem.Modules.Leaves.Models;
using ERPSystem.Modules.MarketingCampaign.Models;
using ERPSystem.Shared.BusinessLogic;
using static ERPSystem.Utils.Constants.General.Route;
using Route = ERPSystem.Utils.Constants.General.Route.MkCampaign;

namespace ERPSystem.Modules.MarketingCampaign
{
    public class MarketingCampaignEndpoint
    {
        public static void Map(RouteGroupBuilder group)
        {

            group.MapGet(Route.ALL_CAMPAIGN,
                async ([AsParameters] MarketingCampaignQuery query, MarketingCampaignService service)
                    => await service.GetAllAsync(query))
                .WithDefaultApiSettings("GetAllCampaigns", "Lista campanii marketing", "GET_CAMPAIGNS", false);

            group.MapGet(Route.CAMPAIGN_BY_ID,
               async (int id, MarketingCampaignService service)
                   => await service.GetByIdAsync(id))
               .WithDefaultApiSettings("GetCampaignById", "Detalii campanie", "GET_CAMPAIGN", true);

            group.MapPost(Route.CREATE_CAMPAIGN,
              async (MarketingCampaignDto dto, MarketingCampaignService service)
                  => await service.CreateAsync(dto))
              .WithDefaultApiSettings("CreateCampaign", "Creare campanie marketing", "CREATE_CAMPAIGN", true);

            group.MapPut(Route.UPDATE_CAMPAIGN,
              async (int id, MarketingCampaignDto dto, MarketingCampaignService service)
                  => await service.UpdateAsync(id, dto))
              .WithDefaultApiSettings("UpdateCampaign", "Actualizează campanie marketing", "UPDATE_CAMPAIGN", true);

            group.MapDelete(Route.DELETE_CAMPAIGN,
               async (int id, MarketingCampaignService service)
                   => await service.DeleteAsync(id))
               .WithDefaultApiSettings("DeleteCampaign", "Șterge campanie marketing", "DELETE_CAMPAIGN", true);

            group.MapPut(Route.CAMPAIGN_STATUS,
                async (int id, MarketingCampaignService service)
                    => await service.ToggleActiveAsync(id))
                .WithDefaultApiSettings("ToggleCampaign", "Activează/Dezactivează campanie", "TOGGLE_CAMPAIGN", true);

            group.MapGet(Route.CAMPAIGN_AVAILABLE,
                async (  int? courseId, int? courseSessionId, DiscountScope scope,MarketingCampaignService service)
                    => await service.GetAvailableCampaignsAsync(courseId, courseSessionId, scope))
                .WithDefaultApiSettings( "GetAvailableCampaigns","Campanii disponibile pentru contract", "GET_AVAILABLE_CAMPAIGNS", true);
        }
    }
}