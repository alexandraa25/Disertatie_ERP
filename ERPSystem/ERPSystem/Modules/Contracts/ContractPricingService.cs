using ERPSystem.Data.Entities;
using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Utils.Enums;

public class ContractPricingService
{
    public decimal CalculateCourseSnapshotPrice(  CourseSession session,  List<CourseSession> allSessions,  StudentContract contract)
    {
        var price = session.Fee;

        foreach (var discount in contract.Discounts)
        {
            switch (discount.Scope)
            {
                case DiscountScope.Package:
                    if (session.FeeType == CourseFeeType.FixedPackage)
                    {
                        var scopeTotal = allSessions
                            .Where(s => s.FeeType == CourseFeeType.FixedPackage)
                            .Sum(s => s.Fee);
                        price = ApplyProportionalOrPercentageDiscount(price, session.Fee, scopeTotal, discount);
                    }
                    break;

                case DiscountScope.Subscription:
                    if (session.FeeType == CourseFeeType.Monthly)
                    {
                        var scopeTotal = allSessions
                            .Where(s => s.FeeType == CourseFeeType.Monthly)
                            .Sum(s => s.Fee);
                        price = ApplyProportionalOrPercentageDiscount(price, session.Fee, scopeTotal, discount);
                    }
                    break;

                case DiscountScope.Total:
                    price = ApplyTotalDiscountToCourse(session, allSessions, contract, discount, price);
                    break;
            }
        }

        return Math.Max(0, Math.Round(price, 2));
    }

    private static decimal ApplyProportionalOrPercentageDiscount(
        decimal price, decimal sessionFee, decimal scopeTotal, ContractDiscount discount)
    {
        if (discount.Type == DiscountType.Percentage)
            return ApplyCourseDiscountValue(price, discount);

        if (scopeTotal <= 0) return price;

        var share = discount.Value * sessionFee / scopeTotal;
        return Math.Max(0, Math.Round(price - share, 2));
    }

    public PricingResult CalculatePricingFromContractCourses(StudentContract contract)
    {
        var result = new PricingResult
        {
            PackageAmount = contract.Courses
                .Where(c => c.FeeType == CourseFeeType.FixedPackage)
                .Sum(c => c.PriceSnapshot),

            MonthlyAmount = contract.Courses
                .Where(c => c.FeeType == CourseFeeType.Monthly)
                .Sum(c => c.PriceSnapshot),

            Months = !contract.IsUnlimited && contract.EndDate.HasValue
                ? ((contract.EndDate.Value.Year - contract.StartDate.Year) * 12)
                  + contract.EndDate.Value.Month
                  - contract.StartDate.Month
                  + 1
                : 0
        };

        result.TotalAmount = contract.IsUnlimited
            ? result.PackageAmount > 0 ? result.PackageAmount : null
            : result.PackageAmount + result.MonthlyAmount * result.Months;

        return result;
    }

    private static decimal ApplyTotalDiscountToCourse( CourseSession session, List<CourseSession> allSessions,  StudentContract contract, ContractDiscount discount, decimal price)
    {
        if (discount.Type == DiscountType.Percentage)
            return ApplyCourseDiscountValue(price, discount);

        var months = !contract.IsUnlimited && contract.EndDate.HasValue
            ? ((contract.EndDate.Value.Year - contract.StartDate.Year) * 12)
              + contract.EndDate.Value.Month
              - contract.StartDate.Month
              + 1
            : 0;

        var packageTotal = allSessions
            .Where(s => s.FeeType == CourseFeeType.FixedPackage)
            .Sum(s => s.Fee);

        var monthlyTotal = allSessions
            .Where(s => s.FeeType == CourseFeeType.Monthly)
            .Sum(s => s.Fee);

        var total = contract.IsUnlimited
            ? packageTotal + monthlyTotal
            : packageTotal + monthlyTotal * months;

        if (total <= 0)
            return price;

        // For unlimited: all courses use face value as weight (months = 0, so can't multiply).
        // For limited monthly: weight = fee * months (total contribution over contract period).
        var courseTotalWeight = (!contract.IsUnlimited && session.FeeType == CourseFeeType.Monthly)
            ? session.Fee * months
            : session.Fee;

        var allocatedDiscount = discount.Value * courseTotalWeight / total;

        // Convert from total-period discount back to per-month discount (limited only).
        if (!contract.IsUnlimited && session.FeeType == CourseFeeType.Monthly && months > 0)
            allocatedDiscount /= months;

        return Math.Max(0, Math.Round(price - allocatedDiscount, 2));
    }

    private static decimal ApplyCourseDiscountValue( decimal amount, ContractDiscount discount)
    {
        if (amount <= 0 || discount.Value <= 0)
            return amount;

        var discounted = discount.Type == DiscountType.Percentage
            ? amount - amount * (discount.Value / 100m)
            : amount - discount.Value;

        return Math.Max(0, Math.Round(discounted, 2));
    }
}