using ERPSystem.Extensions;
using ERPSystem.Modules.Contracts.Models;
using Route = ERPSystem.Utils.Constants.General.Route.Contracts;

namespace ERPSystem.Modules.Contracts;

public static class ContractsEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        // Create Contract
        group.MapPost(Route.CONTRACTS,
            async (CreateContractDto dto, ContractsService service)
                => await service.CreateAsync(dto))
            .WithDefaultApiSettings(
                "CreateContract",
                "Creare contract elev",
                "CREATE",
                false
            );

        // Get Contract by Id
        group.MapGet(Route.CONTRACT_BY_ID,
            async (int id, ContractsService service)
                => await service.GetByIdAsync(id))
            .WithDefaultApiSettings(
                "GetContractById",
                "Detalii contract",
                "GET_BY_ID",
                false
            );

        // List Contracts by Student
        group.MapGet(Route.STUDENT_CONTRACTS,
            async (int id, ContractsService service)
                => await service.ListByStudentAsync(id))
            .WithDefaultApiSettings(
                "GetStudentContracts",
                "Lista contracte elev",
                "LIST_BY_STUDENT",
                false
            );


        group.MapPut(Route.CONTRACT_UPDATE_BODY,
    async (int id, UpdateContractBodyDto dto, ContractsService service)
        => await service.UpdateBodyAsync(id, dto))
    .WithDefaultApiSettings(
        "UpdateContractBody",
        "Editare conținut contract",
        "UPDATE_BODY",
        false
    );

        group.MapPut(Route.CONTRACT_FINALIZE,
    async (int id, ContractsService service)
        => await service.FinalizeAsync(id))
    .WithDefaultApiSettings(
        "FinalizeContract",
        "Finalizare contract",
        "FINALIZE",
        false
    );

    //    group.MapPost(Route.CONTRACT_GENERATE_PDF,
    //async (int id, ContractsService service)
    //    => await service.GeneratePdfAsync(id))
    //.WithDefaultApiSettings(
    //    "GenerateContractPdf",
    //    "Generare PDF contract",
    //    "GENERATE_PDF",
    //    false
    //);

     

        group.MapPut(Route.CONTRACT_CANCEL,
    async (int id, ContractsService service)
        => await service.CancelAsync(id))
    .WithDefaultApiSettings(
        "CancelContract",
        "Anulare contract",
        "CANCEL",
        false
    );

        group.MapGet(Route.CONTRACT_GET_LATEST_BY_STUDENT,
    async (int studentId, ContractsService service)
        => await service.GetLatestByStudentAsync(studentId))
.WithDefaultApiSettings(
    "GetLatestContractByStudent",
    "Returnează ultimul contract al cursantului",
    "READ",
    false
);

        group.MapPost(Route.CONTRACT_SEND_TO_CLIENT,
    async (int id, ContractsService service)
        => await service.SendToClientAsync(id))
.WithDefaultApiSettings(
    "SendContractToClient",
    "Trimite contractul către client pentru semnare",
    "UPDATE",
    false
);

        group.MapPost(Route.CONTRACT_CLIENT_SIGN,
       async (SignContractDto dto, ContractsService service)
           => await service.SignByClientAsync(dto.Token, dto.Signature))
   .WithDefaultApiSettings(
       "ClientSignContract",
       "Semnarea contractului de către client",
       "UPDATE",
       false
   );

        group.MapGet(Route.CONTRACT_GET_FOR_SIGNING,
    async (string token, ContractsService service)
        => await service.GetContractForSigningAsync(token))
.WithDefaultApiSettings(
    "GetContractForSigning",
    "Returnează contractul pentru semnare",
    "READ",
    true
);
        group.MapPost(Route.CONTRACT_ADMIN_SIGN,
    async (int id, AdminSignContractDto dto, ContractsService service)
        => await service.SignByAdminAsync(id, dto.Signature))
.WithDefaultApiSettings(
    "AdminSignContract",
    "Semnarea contractului de către administrator",
    "UPDATE",
    false
);


        group.MapGet(Route.CONTRACT_DOWNLOAD,
    async (int id, ContractsService service) =>
        await service.DownloadContractAsync(id))
.WithDefaultApiSettings(
    "DownloadContract",
    "Descarcă PDF contract",
    "GET",
    false
);

    }





}
