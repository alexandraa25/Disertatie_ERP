using ERPSystem.Extensions;
using ERPSystem.Modules.Payments.Models;
using Route = ERPSystem.Utils.Constants.General.Route.Payments;
namespace ERPSystem.Modules.Payments
{
    public class PaymentsEndpoints
    {
        public static void Map(RouteGroupBuilder group)
        {
            group.MapPost(Route.PAY_INSTALLMENT,
                async (PayInstallmentDto dto, PaymentsService service)
                    => await service.PayInstallmentAsync(dto))
                .RequireAuthorization(policy =>
                   policy.RequireRole("Admin", "Manager", "Accountant", "Secretary"))
               .WithDefaultApiSettings("PayInstallment", "Înregistrează plată rată", "CREATE", false);

            group.MapGet(Route.GET_INSTALLMENTS,
                async (int id, PaymentsService service)
                   => await service.GetInstallments(id))
                .RequireAuthorization(policy =>
                    policy.RequireRole("Admin", "Manager", "Accountant", "Secretary"))
               .WithDefaultApiSettings("GetInstallment", "Returnează ratele contractului", "LIST", false);

            group.MapGet(Route.GET_PAYMENTS,
                async (int id, PaymentsService service)
                   => await service.GetPayments(id))
                .RequireAuthorization(policy =>
                    policy.RequireRole("Admin", "Manager", "Accountant", "Secretary"))
                .WithDefaultApiSettings("GetPayments", "Istoric plati", "LIST", false);

        }
    }
}
