using ERPSystem.Data.Entities;
using ERPSystem.Extensions;
using ERPSystem.Modules.AdditionalAct.Models;
using ERPSystem.Modules.Contracts;
using ERPSystem.Modules.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using Route = ERPSystem.Utils.Constants.General.Route.AdditionalAct;

namespace ERPSystem.Modules.AdditionalAct
{
    public class AdditionalActEndpoints
    {
        public static void Map(RouteGroupBuilder group)
        {
            group.MapPost(Route.CREATE_ACT,
               async ([FromRoute] int contractId, CreateAdditionalActDto dto, [FromServices] AdditionalActService service)
                  => await service.CreateAdditionalActAsync(contractId, dto))
                .RequireAuthorization(policy =>
                  policy.RequireRole("Admin", "Manager", "Accountant"))
               .WithDefaultApiSettings("CreateAdditionalAct", "Creare act aditional", "CREATE", false);

            group.MapPut(Route.UPDATE_BODY,
              async (int id, UpdateAdditionalActBodyDto dto, AdditionalActService service)
                   => await service.UpdateAdditionalActBodyAsync(id, dto))
                .RequireAuthorization(policy =>
                   policy.RequireRole("Admin", "Manager", "Accountant"))
               .WithDefaultApiSettings("UpdateAdditionalActBody", "Update body act aditional", "UPDATE", false);

            group.MapPut(Route.UPDATE_ACT,
                async (int id, CreateAdditionalActDto dto, AdditionalActService service)
                    => await service.UpdateAdditionalActAsync(id, dto))
                .RequireAuthorization(policy =>
                    policy.RequireRole("Admin", "Manager", "Accountant"))
                .WithDefaultApiSettings("UpdateAdditionalAct", "Update act", "UPDATE", false);

            group.MapPost(Route.FINALIZE_ACT,
               async (int id, AdditionalActService service)
                     => await service.FinalizeAdditionalActAsync(id))
                .RequireAuthorization(policy =>
                     policy.RequireRole("Admin", "Manager", "Accountant"))
               .WithDefaultApiSettings("FinalizeAdditionalAct", "Finalizare act aditional", "FINALIZE", false);

            group.MapPost(Route.ACT_SEND_TO_CLIENT,
            async (int id, ContractsService service)
                => await service.SendToClientAsync(SigningEntityType.AdditionalAct, id))
                .RequireAuthorization(policy =>
                  policy.RequireRole("Admin", "Manager", "Accountant"))
           .WithDefaultApiSettings("SendActToClient", "Trimite act aditional către client pentru semnare", "UPDATE", false);

         
            group.MapPost(Route.ACT_ADMIN_SIGN,
                async (int id, AdminSignContractDto dto, ContractsService service)
                    => await service.SignByAdminAsync(SigningEntityType.AdditionalAct, id, dto.Signature))
                .RequireAuthorization(policy =>
                    policy.RequireRole("Admin", "Manager", "Accountant"))
               .WithDefaultApiSettings("AdminSignAct", "Semnarea act aditional de către administrator", "UPDATE", false);

            group.MapGet(Route.DOWNLOAD_ACT, 
                async (int id, AdditionalActService service)
                    => await service.DownloadActAsync(id))
                .RequireAuthorization(policy =>
                    policy.RequireRole("Admin", "Manager", "Accountant"))
               .WithDefaultApiSettings("DownloadAct", "Descarcă act adițional", "GET", false);

            group.MapDelete(Route.DELETE_ACT,
                async (int id, AdditionalActService service)
                    => await service.DeleteAdditionalActAsync(id))
                .RequireAuthorization(policy =>
                    policy.RequireRole("Admin", "Manager", "Accountant"))
                .WithDefaultApiSettings( "DeleteAdditionalAct", "Ștergere act adițional", "DELETE",  false);

            group.MapGet(Route.LIST_ACT,
                async ([FromRoute] int contractId, AdditionalActService service)
                     => await service.ListAdditionalActsAsync(contractId))
                .RequireAuthorization(policy =>
                     policy.RequireRole("Admin", "Manager", "Accountant"))
                .WithDefaultApiSettings("ListAdditionalActs", "Lista acte aditionale pe contract", "GET", false);

            group.MapGet(Route.GET_BY_ID,
               async (int id, AdditionalActService service)
                    => await service.GetAdditionalActByIdAsync(id))
                .RequireAuthorization(policy =>
                   policy.RequireRole("Admin", "Manager", "Accountant"))
               .WithDefaultApiSettings("GetAdditionalActById", "Detalii act adițional", "GET", false);


        }
    }
}
