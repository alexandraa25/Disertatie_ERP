using ERPSystem.Extensions;

using ERPSystem.Modules.Admin;
using ERPSystem.Modules.Employees;

using ERPSystem.Modules.Leaves.Models;
using ERPSystem.Shared.BusinessLogic;
using static ERPSystem.Utils.Constants.General.Route;
using Route = ERPSystem.Utils.Constants.General.Route.Leaves;

namespace ERPSystem.Modules.Leaves
{
    public class LeavesEndpoints
    {
        public static void Map(RouteGroupBuilder group)
        {

            group.MapGet(Route.MY_LEAVES,
                 async (LeavesService service)
                     => await service.GetMyLeaves())
                 .WithDefaultApiSettings("GetMyLeaves", "Concediile utilizatorului", "GET_MY_LEAVES", true);

            group.MapPost(Route.CREATE_LEAVES,
                 async (CreateLeaveDto request, LeavesService service)
                     => await service.CreateLeave(request))
                 .WithDefaultApiSettings("CreateLeave", "Creare cerere concediu", "CREATE_LEAVE", true);

            group.MapPut(Route.UPDATE_LEAVES,
                 async (Guid id, CreateLeaveDto dto, LeavesService service)
                     => await service.UpdateLeave(id, dto))
                 .WithDefaultApiSettings("UpdateLeave", "Actualizează concediu", "UPDATE_LEAVE", true);

            group.MapPut(Route.CANCEL_LEAVES,
                  async (Guid id, LeavesService service)
                      => await service.CancelLeave(id))
                  .WithDefaultApiSettings("CancelLeave", "Anulează concediu", "CANCEL_LEAVE", true);

            group.MapPut(Route.APPROVE_LEAVES,
                 async (Guid id, LeavesService service)
                     => await service.Approve(id))
                 .WithDefaultApiSettings("ApproveLeave", "Aprobare concediu", "APPROVE_LEAVE", false);

            group.MapPut(Route.REJECT_LEAVES,
                async (Guid id, string? reason, LeavesService service)
                    => await service.Reject(id, reason))
                .WithDefaultApiSettings("RejectLeave", "Respingere concediu", "REJECT_LEAVE", false);

            group.MapGet(Route.ALL_LEAVES,
                 async ([AsParameters] GetLeavesQuery query, LeavesService service)
                      => await service.GetAllLeaves(query))
                 .WithDefaultApiSettings("GetAllLeaves", "Toate concediile", "GET_ALL_LEAVES", true);

            group.MapGet(Route.GET_CONFLICTS,
                 async (DateTime start, DateTime end, Guid? excludeId, LeavesService service)
                     => await service.GetConflicts(start, end, excludeId))
                 .WithDefaultApiSettings("GetConflicts", "Verifică suprapuneri concedii", "GET_CONFLICTS", true);

            group.MapGet(Route.HOLIDAYS,
                  async (int year, LeavesService service)
                       => await service.GetHolidays(year))
                 .WithDefaultApiSettings( "GetHolidays",  "Sarbatori legale pe an", "GET_HOLIDAYS", false);

         

            group.MapGet(Route.EXPORT_LEAVES, async (LeavesService service) =>
            {
                var file = await service.ExportLeaves();

                return Results.File(file,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "leaves.xlsx");
            });
        }
    }
}
