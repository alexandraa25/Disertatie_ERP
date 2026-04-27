
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
                async (int id, ToggleCampaignRequest request, MarketingCampaignService service)
                     => await service.ToggleActiveAsync(id, request.EndDate))
                .WithDefaultApiSettings("ToggleCampaign", "Activează/Dezactivează campanie", "TOGGLE_CAMPAIGN", true);

            group.MapPost(Route.CAMPAIGN_AVAILABLE,
                  async ([FromBody] AvailableCampaignsRequest request, MarketingCampaignService service)
                     => await service.GetAvailableCampaignsAsync(request.CourseSessionIds))
                .WithDefaultApiSettings( "GetAvailableCampaigns","Campanii disponibile pentru contract", "GET_AVAILABLE_CAMPAIGNS", true);

            group.MapPost(Route.SEND_NEWSLETTER,
                  async ([FromBody] SendCampaignNewsletterRequest request, MarketingCampaignService service)
                      => await service.SendNewsletterAsync(request))
                  .WithDefaultApiSettings( "SendNewsletter", "Trimite newsletter pentru campanie","SEND_CAMPAIGN_NEWSLETTER", true);

            group.MapGet(Route.CAMPAIGN_NEWSLETTER_TEMPLATE,
                  async (int campaignId, MarketingCampaignService service)
                      => await service.GetCampaignNewsletterTemplateAsync(campaignId))
                  .WithDefaultApiSettings(  "GetCampaignNewsletterTemplate", "Template newsletter campanie", "GET_CAMPAIGN_NEWSLETTER_TEMPLATE", true );

            group.MapPost(Route.EMAIL_LOGS,
                  async ([FromBody] EmailLogsRequest request, MarketingCampaignService service)
                      => await service.GetEmailLogsAsync(request))
                  .WithDefaultApiSettings( "GetEmailLogs", "Listare emailuri trimise",  "GET_EMAIL_LOGS",true );

            group.MapGet(Route.EMAIL_LOG_DETAILS,
                async (int id, MarketingCampaignService service)
                    => await service.GetEmailLogDetailsAsync(id))
                .WithDefaultApiSettings( "GetEmailLogDetails", "Detalii email trimis","GET_EMAIL_LOG_DETAILS", true  );
        }
    }
}