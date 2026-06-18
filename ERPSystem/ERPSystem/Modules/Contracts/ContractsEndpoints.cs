using ERPSystem.Data.Entities;
using ERPSystem.Extensions;
using ERPSystem.Modules.AdditionalAct.Models;
using ERPSystem.Modules.Contracts.Models;
using Route = ERPSystem.Utils.Constants.General.Route.Contracts;

namespace ERPSystem.Modules.Contracts;

public static class ContractsEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost(Route.CONTRACTS,
            async (CreateContractDto dto, ContractsService service)
                => await service.CreateAsync(dto))
            .RequireAuthorization(policy =>
                policy.RequireRole("Admin", "Manager", "Accountant", "Secretary"))
            .WithDefaultApiSettings("CreateContract", "Creare contract elev", "CREATE", false);

        group.MapGet(Route.CONTRACT_BY_ID,
            async (int id, ContractsService service)
                => await service.GetByIdAsync(id))
            .RequireAuthorization(policy =>
                policy.RequireRole("Admin", "Manager", "Accountant", "Secretary"))
            .WithDefaultApiSettings("GetContractById", "Detalii contract", "GET_BY_ID", false );

        group.MapGet(Route.STUDENT_CONTRACTS,
            async (int id, ContractsService service)
                => await service.ListByStudentAsync(id))
            .RequireAuthorization(policy =>
                policy.RequireRole("Admin", "Manager", "Accountant", "Secretary"))
            .WithDefaultApiSettings( "GetStudentContracts", "Lista contracte elev", "LIST_BY_STUDENT", false);

        group.MapPut(Route.CONTRACT_UPDATE_BODY,
            async (int id, UpdateContractBodyDto dto, ContractsService service)
                => await service.UpdateBodyAsync(id, dto.ContractBody))
            .RequireAuthorization(policy =>
                policy.RequireRole("Admin", "Manager", "Accountant", "Secretary"))
           .WithDefaultApiSettings("UpdateContractBody", "Editare conținut contract", "UPDATE_BODY", false);

        group.MapPut(Route.CONTRACT_UPDATE,
            async (int id, UpdateContractDto dto, ContractsService service)
                => await service.UpdateAsync(id, dto))
            .RequireAuthorization(policy =>
                 policy.RequireRole("Admin", "Manager", "Accountant", "Secretary"))
            .WithDefaultApiSettings( "UpdateContract", "Actualizare contract (Draft)", "UPDATE", false );

        group.MapPut(Route.RESET_BODY,
             async (int id, ContractsService service)
                 => await service.ResetBodyAsync(id))
            .RequireAuthorization(policy =>
                 policy.RequireRole("Admin", "Manager", "Accountant", "Secretary"))
             .WithDefaultApiSettings( "ResetContractBody", "Reset contract la template", "UPDATE", false );

        group.MapPut(Route.CONTRACT_FINALIZE,
             async (int id, ContractsService service)
                 => await service.FinalizeAsync(id))
            .RequireAuthorization(policy =>
                 policy.RequireRole("Admin", "Manager", "Accountant"))
             .WithDefaultApiSettings( "FinalizeContract", "Finalizare contract", "FINALIZE", false );

        group.MapPut(Route.CONTRACT_CANCEL,
             async (int id, ContractsService service)
                 => await service.CancelAsync(id))
             .RequireAuthorization(policy =>
                 policy.RequireRole("Admin", "Manager", "Accountant"))
             .WithDefaultApiSettings( "CancelContract","Anulare contract", "CANCEL",false );

        group.MapPost(Route.CONTRACT_SEND_TO_CLIENT,
             async (int id, ContractsService service)
                 => await service.SendToClientAsync(SigningEntityType.Contract, id))
             .RequireAuthorization(policy =>
                 policy.RequireRole("Admin", "Manager", "Accountant"))
            .WithDefaultApiSettings("SendContractToClient","Trimite contractul către client pentru semnare","UPDATE", false);

        group.MapPost(Route.DOCUMENT_CLIENT_SIGN,
            async (SignContractDto dto, ContractsService service)
                 => await service.SignByClientAsync(dto.Token, dto.Signature))
            .WithDefaultApiSettings( "ClientSignContract", "Semnarea contractului de către client","UPDATE",false);

        group.MapGet(Route.DOCUMENT_GET_FOR_SIGNING,
            async (string token, ContractsService service)
                 => await service.GetContractForSigningAsync(token))
            .WithDefaultApiSettings( "GetContractForSigning","Returnează contractul pentru semnare","READ", true);

        group.MapPost(Route.CONTRACT_ADMIN_SIGN,
            async (int id, AdminSignContractDto dto, ContractsService service)
                => await service.SignByAdminAsync(SigningEntityType.Contract, id, dto.Signature))
            .RequireAuthorization(policy =>
                policy.RequireRole("Admin", "Manager", "Accountant"))
           .WithDefaultApiSettings("AdminSignContract", "Semnarea contractului de către administrator", "UPDATE",false);

        group.MapGet(Route.CONTRACT_DOWNLOAD,
           async (int id, ContractsService service) 
                => await service.DownloadContractAsync(id))
            .RequireAuthorization(policy =>
                policy.RequireRole("Admin", "Manager", "Accountant", "Secretary"))
           .WithDefaultApiSettings( "DownloadContract","Descarcă PDF contract","GET", false);

        group.MapPut(Route.CONTRACT_COMPLETE,
            async (int id, ContractsService service)
                => await service.CompleteAsync(id))
            .RequireAuthorization(policy =>
               policy.RequireRole("Admin", "Manager", "Accountant"))
            .WithDefaultApiSettings( "CompleteContract", "Finalizează contractul (normal)","UPDATE", false);

        group.MapPost(Route.CONTRACT_EXPIRE,
           async (ContractsService service) 
                =>{await service.ExpireContractsAsync(); return Results.Ok();})
          .WithDefaultApiSettings( "ExpireContractsJob", "Rulează expirarea contractelor","SYSTEM",false);

        group.MapDelete(Route.CONTRACT_DELETE,
            async (int id, ContractsService service)
                => await service.DeleteAsync(id))
            .RequireAuthorization(policy =>
                policy.RequireRole("Admin", "Manager", "Accountant"))
            .WithDefaultApiSettings("DeleteDraftContract", "Ștergere contract Draft", "DELETE", false);

        group.MapGet(Route.GET_CONTRACTS_OVERVIEW,
           async (ContractsService service)
               => await service.GetContractsOverviewAsync())
            .RequireAuthorization(policy =>
               policy.RequireRole("Admin", "Manager", "Accountant", "Secretary"))
           .WithDefaultApiSettings("GetContractsOverview","Returnează toate contractele cu actele adiționale aferente", "GET_CONTRACTS_OVERVIEW",  true );

        group.MapGet(Route.EXPORT_CONTRACTS_OVERVIEW,
            async (DateTime? from, DateTime? to, ContractsService service)
                 => await service.ExportContractsExcelAsync(from, to))
            .RequireAuthorization(policy =>
                 policy.RequireRole("Admin", "Manager", "Accountant"))
            .WithDefaultApiSettings( "ExportContracts", "Export contracte pentru contabil", "EXPORT_CONTRACTS", true);

    

         
    }

}
