using ERPSystem.Extensions;
using ERPSystem.Modules.Contracts;
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
               .WithDefaultApiSettings("PayInstallment", "Plată rată", "CREATE", false);

            group.MapGet(Route.GET_INSTALLMENTS,
                async (int id, PaymentsService service)
                   => await service.GetInstallments(id))
               .WithDefaultApiSettings("GetInstallment", "Rate", "LIST", false);

            group.MapGet(Route.GET_PAYMENTS,
                async (int id, PaymentsService service)
                   => await service.GetPayments(id))
                .WithDefaultApiSettings("GetPayments", "Istoric plati", "LIST", false);

        }
    }
}
