using ERPSystem.Data.Entities;
using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Utils.Enums;

public class ContractPricingService
{
    public PricingResult CalculatePricing( List<CourseSession> sessions,  StudentContract contract)
    {
        var result = new PricingResult
        {
            PackageAmount = sessions
                .Where(s => s.FeeType == CourseFeeType.FixedPackage)
                .Sum(s => s.Fee),

            MonthlyAmount = sessions
                .Where(s => s.FeeType == CourseFeeType.Monthly)
                .Sum(s => s.Fee),

            Months = !contract.IsUnlimited && contract.EndDate.HasValue
                ? ((contract.EndDate.Value.Year - contract.StartDate.Year) * 12)
                  + contract.EndDate.Value.Month
                  - contract.StartDate.Month
                  + 1
                : 0
        };

        foreach (var adjustment in contract.PriceAdjustments ?? new List<ContractPriceAdjustment>())
        {
            ApplyPriceAdjustment(result, adjustment, sessions);
        }

        foreach (var discount in contract.Discounts ?? new List<ContractDiscount>())
        {
            ApplyDiscount(result, discount, contract.IsUnlimited);
        }

        result.TotalAmount = contract.IsUnlimited
            ? result.PackageAmount > 0 ? result.PackageAmount : null
            : result.PackageAmount + result.MonthlyAmount * result.Months;

        return result;
    }

    private static void ApplyDiscount( PricingResult result, ContractDiscount discount, bool isUnlimited)
    {
        switch (discount.Scope)
        {
            case DiscountScope.Package:
                result.PackageAmount = ApplyDiscountValue(result.PackageAmount, discount, result);
                break;

            case DiscountScope.Subscription:
                result.MonthlyAmount = ApplyDiscountValue(result.MonthlyAmount, discount, result);
                break;

            case DiscountScope.Total:
                if (isUnlimited)
                {
                    if (result.PackageAmount > 0)
                        result.PackageAmount = ApplyDiscountValue(result.PackageAmount, discount, result);

                    if (result.MonthlyAmount > 0)
                        result.MonthlyAmount = ApplyDiscountValue(result.MonthlyAmount, discount, result);
                }
                else
                {
                    var total = result.PackageAmount + result.MonthlyAmount * result.Months;
                    var discountedTotal = ApplyDiscountValue(total, discount, result);

                    if (total > 0)
                    {
                        var ratio = discountedTotal / total;

                        result.PackageAmount = Math.Round(result.PackageAmount * ratio, 2);
                        result.MonthlyAmount = Math.Round(result.MonthlyAmount * ratio, 2);
                    }
                }

                break;
        }
    }

    private static decimal ApplyDiscountValue( decimal amount, ContractDiscount discount, PricingResult result)
    {
        if (amount <= 0 || discount.Value <= 0)
            return amount;

        var discounted = discount.Type == DiscountType.Percentage
            ? amount - amount * (discount.Value / 100m)
            : amount - discount.Value;

        var safeDiscounted = Math.Max(0, Math.Round(discounted, 2));
        var diff = amount - safeDiscounted;

        if (diff > 0)
            result.DiscountTotal += diff;

        return safeDiscounted;
    }

    private static void ApplyPriceAdjustment(  PricingResult result, ContractPriceAdjustment adjustment, List<CourseSession> sessions)
    {
        if (adjustment.Amount <= 0)
            return;

        var session = sessions.FirstOrDefault(s => s.Id == adjustment.CourseSessionId);

        if (session == null)
            return;

        var signedAmount = adjustment.Type == PriceAdjustmentType.Increase
            ? adjustment.Amount
            : -adjustment.Amount;

        if (session.FeeType == CourseFeeType.Monthly)
        {
            result.MonthlyAmount = Math.Max(0, result.MonthlyAmount + signedAmount);
        }
        else
        {
            result.PackageAmount = Math.Max(0, result.PackageAmount + signedAmount);
        }

        if (adjustment.Type == PriceAdjustmentType.Discount)
        {
            result.DiscountTotal += adjustment.Amount;
        }
    }
}