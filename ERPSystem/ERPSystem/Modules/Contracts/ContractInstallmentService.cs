using ERPSystem.Data.Entities;
using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Utils.Enums;

public class ContractInstallmentService
{
    public List<ContractInstallment> BuildInstallments( DateTime startDate,  bool isUnlimited, int packageInstallments, PricingResult pricing)
    {
        var result = new List<ContractInstallment>();

        packageInstallments = packageInstallments <= 0 ? 1 : packageInstallments;

        var hasPackage = pricing.PackageAmount > 0;
        var hasSubscription = pricing.MonthlyAmount > 0;

        var packageInstallmentValue = hasPackage
            ? Math.Floor((pricing.PackageAmount / packageInstallments) * 100) / 100
            : 0;

        var totalAssigned = packageInstallmentValue * packageInstallments;
        var remainder = pricing.PackageAmount - totalAssigned;

        var months = isUnlimited
            ? hasPackage ? packageInstallments : 1
            : pricing.Months > 0 ? pricing.Months : packageInstallments;

        for (var i = 0; i < months; i++)
        {
            decimal amount = 0;

            if (hasSubscription)
                amount += pricing.MonthlyAmount;

            if (hasPackage && i < packageInstallments)
            {
                var packagePart = packageInstallmentValue;

                if (i == packageInstallments - 1)
                    packagePart += remainder;

                amount += packagePart;
            }

            result.Add(new ContractInstallment
            {
                DueDate = startDate.AddMonths(i),
                Amount = Math.Round(amount, 2),
                PaidAmount = 0,
                Status = InstallmentStatus.Pending
            });
        }

        return result;
    }

    public List<ContractInstallment> BuildInstallmentsForRemainingPeriod( DateTime startDate,  DateTime contractEndDate, bool isUnlimited, PricingResult pricing)
    {
        var result = new List<ContractInstallment>();

        startDate = startDate.Date;
        contractEndDate = contractEndDate.Date;

        if (!isUnlimited && contractEndDate < startDate)
            return result;

        var months = isUnlimited
            ? 1
            : ((contractEndDate.Year - startDate.Year) * 12)
              + contractEndDate.Month
              - startDate.Month
              + 1;

        months = Math.Max(1, months);

        for (var i = 0; i < months; i++)
        {
            var dueDate = startDate.AddMonths(i);

            decimal amount = 0;

            if (pricing.MonthlyAmount > 0)
                amount += pricing.MonthlyAmount;

            result.Add(new ContractInstallment
            {
                DueDate = dueDate,
                Amount = Math.Round(amount, 2),
                PaidAmount = 0,
                Status = InstallmentStatus.Pending
            });
        }

        return result;
    }
}