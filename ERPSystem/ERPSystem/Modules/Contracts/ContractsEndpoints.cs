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

        // Activate Contract
        group.MapPut(Route.CONTRACT_ACTIVATE,
            async (int id, ContractsService service)
                => await service.ActivateAsync(id))
            .WithDefaultApiSettings(
                "ActivateContract",
                "Activare contract",
                "ACTIVATE",
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

        group.MapPost(Route.CONTRACT_GENERATE_PDF,
    async (int id, ContractsService service)
        => await service.GeneratePdfAsync(id))
    .WithDefaultApiSettings(
        "GenerateContractPdf",
        "Generare PDF contract",
        "GENERATE_PDF",
        false
    );

        group.MapPut(Route.CONTRACT_SIGN,
    async (int id, ContractsService service)
        => await service.SignAsync(id))
    .WithDefaultApiSettings(
        "SignContract",
        "Semnare contract",
        "SIGN",
        false
    );

        group.MapPut(Route.CONTRACT_CANCEL,
    async (int id, ContractsService service)
        => await service.CancelAsync(id))
    .WithDefaultApiSettings(
        "CancelContract",
        "Anulare contract",
        "CANCEL",
        false
    );
    }
}
