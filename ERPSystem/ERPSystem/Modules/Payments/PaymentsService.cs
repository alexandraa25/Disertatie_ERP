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
            var contract = await _db.StudentContracts
                .Include(c => c.InstallmentsList)
                .Include(c => c.AdditionalActs)
                    .ThenInclude(a => a.Items)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
                return new List<InstallmentDto>();

            // 🔥 1. CALCUL STATE
            var monthly = contract.InstallmentsList
                .OrderBy(i => i.DueDate)
                .Select(i => i.Amount)
                .FirstOrDefault();

            var total = contract.TotalAmount;
            var endDate = contract.EndDate;

            var appliedActs = contract.AdditionalActs
                .Where(a => a.Status == AdditionalActStatus.Applied)
                .ToList();

            foreach (var act in appliedActs)
            {
                foreach (var item in act.Items)
                {
                    switch (item.Type)
                    {
                        case AdditionalActType.AddCourse:
                            monthly += decimal.Parse(item.NewValue ?? "0");
                            if (total.HasValue) total += decimal.Parse(item.NewValue ?? "0");
                            break;

                        case AdditionalActType.RemoveCourse:
                            monthly -= decimal.Parse(item.NewValue ?? "0");
                            if (total.HasValue) total -= decimal.Parse(item.NewValue ?? "0");
                            break;

                        case AdditionalActType.ChangePrice:
                            if (total.HasValue)
                                total = decimal.Parse(item.NewValue ?? "0");
                            else
                                monthly = decimal.Parse(item.NewValue ?? "0");
                            break;

                        case AdditionalActType.ExtendPeriod:
                            if (DateTime.TryParse(item.NewValue, out var newEnd))
                                endDate = newEnd;
                            break;
                    }
                }
            }

            // 🔥 2. GENEREZI RATE
            var result = new List<InstallmentDto>();

            if (total == null)
            {
                // abonament → doar lunar
                result.AddRange(contract.InstallmentsList.Select(i => new InstallmentDto
                {
                    Id = i.Id, // 🔥 IMPORTANT
                    DueDate = i.DueDate,
                    Amount = monthly,
                    PaidAmount = i.PaidAmount
                }));
            }
            else
            {
                var months = CalculateMonths(contract.StartDate, endDate ?? contract.StartDate);

                for (int i = 0; i < months; i++)
                {
                    var due = contract.StartDate.AddMonths(i);

                    var existing = contract.InstallmentsList
                        .FirstOrDefault(x => x.DueDate.Month == due.Month && x.DueDate.Year == due.Year);

                    result.Add(new InstallmentDto
                    {
                        Id = existing?.Id ?? 0,
                        DueDate = due,
                        Amount = monthly,
                        PaidAmount = existing?.PaidAmount ?? 0
                    });
                }
            }

            return result;
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