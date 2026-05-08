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

        public PaymentsService(ApplicationDbContext db, ILogger<PaymentsService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<PublicResponse> PayInstallmentAsync(PayInstallmentDto dto)
        {
            var response = new PublicResponse(true);

            if (dto.Amount <= 0)
                return response.SetError(ErrorCodes.InvalidParameters, "Suma invalidă");

            var installment = await _db.ContractInstallments
                .Include(i => i.Contract)
                .FirstOrDefaultAsync(i => i.Id == dto.InstallmentId);

            if (installment == null)
                return response.SetError(ErrorCodes.InvalidParameters, "Rata nu există");

            if (installment.Contract.Status != ContractStatus.Active)
                return response.SetError(ErrorCodes.InvalidParameters, "Contractul nu este activ");

            var remaining = installment.Amount - installment.PaidAmount;

            if (remaining <= 0)
                return response.SetError(ErrorCodes.InvalidParameters, "Rata este deja plătită");

            // 🔥 nu permite overpay pe rată (poți schimba dacă vrei)
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
    }
}