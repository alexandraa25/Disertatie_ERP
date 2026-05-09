using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Modules.Payments.Models;
using ERPSystem.Shared.BusinessLogic;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Enums;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Modules.Payments
{
    public class PaymentsService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PaymentsService> _logger;
        private readonly ContractPricingService _pricingService;

        public PaymentsService(ApplicationDbContext db, ILogger<PaymentsService> logger, ContractPricingService pricingService)
        {
            _db = db;
            _logger = logger;
            _pricingService = pricingService;
        }

        public async Task<PublicResponse> PayInstallmentAsync(PayInstallmentDto dto)
        {
            var response = new PublicResponse(true);

            if (dto.Amount <= 0)
                return response.SetError(ErrorCodes.InvalidParameters, "Suma invalidă");

            var installment = await _db.ContractInstallments
                .Include(i => i.Contract)
                .FirstOrDefaultAsync(i => i.Id == dto.InstallmentId);

            if (installment.Status == InstallmentStatus.Paid)
                return response.SetError(
                    ErrorCodes.InvalidParameters,
                    "Rata este deja plătită"
                );

            if (installment.Status == InstallmentStatus.Cancelled ||
                installment.Status == InstallmentStatus.Expired)
            {
                return response.SetError(ErrorCodes.InvalidParameters, "Rata nu mai poate fi plătită");
            }

            var remaining = installment.Amount - installment.PaidAmount;

            if (remaining <= 0)
                return response.SetError(ErrorCodes.InvalidParameters, "Rata este deja plătită");

            var amountToApply = Math.Min(dto.Amount, remaining);

            installment.PaidAmount += amountToApply;

            var payment = new Payment
            {
                ContractId = installment.ContractId,
                InstallmentId = installment.Id,
                Amount = amountToApply,
                Method = dto.Method,
                Notes = dto.Notes,
                Reference = dto.Reference,
                PaidAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                Status = "Completed"
            };

            if (installment.PaidAmount >= installment.Amount)
            {
                installment.Status = InstallmentStatus.Paid;
            }

            _db.Payments.Add(payment);

            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = nameof(ContractInstallment),
                EntityId = installment.Id.ToString(),
                Action = "Payment",
                Description = $"Plată {amountToApply} lei pentru rata #{installment.Id}",
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return response.SetSuccess(true);
        }

        public async Task<List<InstallmentDto>> GetInstallments(int contractId)
        {
            await GenerateNextUnlimitedInstallmentForContractAsync(contractId);

            return await _db.ContractInstallments
                .AsNoTracking()
                .Where(i => i.ContractId == contractId)
                .OrderBy(i => i.DueDate)
                .Select(i => new InstallmentDto
                {
                    Id = i.Id,
                    DueDate = i.DueDate,
                    Amount = i.Amount,
                    PaidAmount = i.PaidAmount,
                    Status = i.Status.ToString()
                })
                .ToListAsync();
        }

        public async Task<List<Payment>> GetPayments(int contractId)
        {
            return await _db.Payments
                .Include(p => p.Installment)
                .Where(p => p.ContractId == contractId)
                .OrderByDescending(p => p.PaidAtUtc)
                .ToListAsync();
        }

        public string GetInstallmentStatus(ContractInstallment i)
        {
            if (i.PaidAmount == 0)
                return "Neplătit";

            if (i.PaidAmount < i.Amount)
                return "Parțial";

            return "Plătit";
        }

        public bool IsOverdue(ContractInstallment i)
        {
            return i.PaidAmount < i.Amount &&
                   i.DueDate < DateTime.UtcNow;
        }


        private int CalculateMonths(DateTime start, DateTime end)
        {
            return (end.Year - start.Year) * 12 +
                   (end.Month - start.Month) + 1;
        }

        private async Task GenerateNextUnlimitedInstallmentForContractAsync(int contractId)
        {
            var contract = await _db.StudentContracts
                .Include(c => c.Courses)
                .Include(c => c.InstallmentsList)
                .FirstOrDefaultAsync(c =>
                    c.Id == contractId &&
                    c.IsUnlimited &&
                    c.Status == ContractStatus.Active);

            if (contract == null)
                return;

            var pricing = _pricingService.CalculatePricingFromContractCourses(contract);

            if (pricing.MonthlyAmount <= 0)
                return;

            var today = DateTime.UtcNow.Date;

            var orderedInstallments = contract.InstallmentsList
                .OrderBy(i => i.DueDate)
                .ToList();

            var firstDueDate = orderedInstallments
                .Select(i => (DateTime?)i.DueDate.Date)
                .FirstOrDefault() ?? contract.StartDate.Date;

            var lastDueDate = orderedInstallments
                .Select(i => (DateTime?)i.DueDate.Date)
                .LastOrDefault();

            var nextDueDate = lastDueDate.HasValue
                ? AddMonthKeepingDay(lastDueDate.Value, firstDueDate.Day)
                : firstDueDate;

            if (nextDueDate > today)
                return;

            var alreadyExists = contract.InstallmentsList.Any(i =>
                i.DueDate.Year == nextDueDate.Year &&
                i.DueDate.Month == nextDueDate.Month);

            if (alreadyExists)
                return;

            contract.InstallmentsList.Add(new ContractInstallment
            {
                ContractId = contract.Id,
                DueDate = nextDueDate,
                Amount = Math.Round(pricing.MonthlyAmount, 2),
                PaidAmount = 0,
                Status = InstallmentStatus.Pending
            });

            await _db.SaveChangesAsync();
        }

        private static DateTime AddMonthKeepingDay(DateTime date, int targetDay)
        {
            var nextMonth = date.AddMonths(1);
            var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
            var day = Math.Min(targetDay, daysInMonth);

            return new DateTime(nextMonth.Year, nextMonth.Month, day);
        }
    }
}