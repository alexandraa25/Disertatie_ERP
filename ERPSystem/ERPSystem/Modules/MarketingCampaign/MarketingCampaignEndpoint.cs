
using ERPSystem.Extensions;
using ERPSystem.Modules.MarketingCampaign.Models;
using Microsoft.AspNetCore.Mvc;
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
                .RequireAuthorization(policy =>
                    policy.RequireRole("Admin", "Manager", "Marketing"))
                .WithDefaultApiSettings("GetAllCampaigns", "Lista campanii marketing", "GET_CAMPAIGNS", false);

            group.MapGet(Route.CAMPAIGN_BY_ID,
               async (int id, MarketingCampaignService service)
                   => await service.GetByIdAsync(id))
                .RequireAuthorization(policy =>
                  policy.RequireRole("Admin", "Manager", "Marketing"))
               .WithDefaultApiSettings("GetCampaignById", "Detalii campanie", "GET_CAMPAIGN", true);

            group.MapPost(Route.CREATE_CAMPAIGN,
              async (MarketingCampaignDto dto, MarketingCampaignService service)
                  => await service.CreateAsync(dto))
                .RequireAuthorization(policy =>
                  policy.RequireRole("Admin", "Manager", "Marketing"))
              .WithDefaultApiSettings("CreateCampaign", "Creare campanie marketing", "CREATE_CAMPAIGN", true);

            group.MapPut(Route.UPDATE_CAMPAIGN,
              async (int id, MarketingCampaignDto dto, MarketingCampaignService service)
                  => await service.UpdateAsync(id, dto))
              .RequireAuthorization(policy =>
                  policy.RequireRole("Admin", "Manager", "Marketing"))
              .WithDefaultApiSettings("UpdateCampaign", "Actualizează campanie marketing", "UPDATE_CAMPAIGN", true);

            group.MapDelete(Route.DELETE_CAMPAIGN,
               async (int id, MarketingCampaignService service)
                   => await service.DeleteAsync(id))
              .RequireAuthorization(policy =>
                  policy.RequireRole("Admin", "Manager", "Marketing")) 
               .WithDefaultApiSettings("DeleteCampaign", "Șterge campanie marketing", "DELETE_CAMPAIGN", true);

            group.MapPut(Route.CAMPAIGN_STATUS,
                async (int id, ToggleCampaignRequest request, MarketingCampaignService service)
                     => await service.ToggleActiveAsync(id, request.EndDate))
                .RequireAuthorization(policy =>
                    policy.RequireRole("Admin", "Manager", "Marketing"))
                .WithDefaultApiSettings("ToggleCampaign", "Activează/Dezactivează campanie", "TOGGLE_CAMPAIGN", true);

            group.MapPost(Route.CAMPAIGN_AVAILABLE,
                  async ([FromBody] AvailableCampaignsRequest request, MarketingCampaignService service)
                     => await service.GetAvailableCampaignsAsync(request.CourseSessionIds))
                  .RequireAuthorization(policy =>
                     policy.RequireRole("Admin", "Manager", "Marketing", "Accountant", "Secretary"))
                .WithDefaultApiSettings( "GetAvailableCampaigns","Campanii disponibile pentru contract", "GET_AVAILABLE_CAMPAIGNS", true);

            group.MapPost(Route.SEND_NEWSLETTER,
                  async ([FromBody] SendCampaignNewsletterRequest request, MarketingCampaignService service)
                      => await service.SendNewsletterAsync(request))
                .RequireAuthorization(policy =>
                     policy.RequireRole("Admin", "Manager", "Marketing"))
                  .WithDefaultApiSettings( "SendNewsletter", "Trimite newsletter pentru campanie","SEND_CAMPAIGN_NEWSLETTER", true);

            group.MapGet(Route.CAMPAIGN_NEWSLETTER_TEMPLATE,
                  async (int campaignId, MarketingCampaignService service)
                      => await service.GetCampaignNewsletterTemplateAsync(campaignId))
                  .RequireAuthorization(policy =>
                      policy.RequireRole("Admin", "Manager", "Marketing"))
                  .WithDefaultApiSettings(  "GetCampaignNewsletterTemplate", "Template newsletter campanie", "GET_CAMPAIGN_NEWSLETTER_TEMPLATE", true );

            group.MapPost(Route.EMAIL_LOGS,
                  async ([FromBody] EmailLogsRequest request, MarketingCampaignService service)
                      => await service.GetEmailLogsAsync(request))
                .RequireAuthorization(policy =>
                      policy.RequireRole("Admin", "Manager", "Marketing"))
                  .WithDefaultApiSettings( "GetEmailLogs", "Listare emailuri trimise",  "GET_EMAIL_LOGS",true );

            group.MapGet(Route.EMAIL_LOG_DETAILS,
                async (int id, MarketingCampaignService service)
                    => await service.GetEmailLogDetailsAsync(id))
                .RequireAuthorization(policy =>
                    policy.RequireRole("Admin", "Manager", "Marketing"))
                .WithDefaultApiSettings( "GetEmailLogDetails", "Detalii email trimis","GET_EMAIL_LOG_DETAILS", true  );
        }
    }
}