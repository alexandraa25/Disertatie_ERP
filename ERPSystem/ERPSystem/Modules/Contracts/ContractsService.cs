using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.AdditionalAct;

using ERPSystem.Modules.Contracts.Models;
using ERPSystem.Shared.BusinessLogic;
using ERPSystem.Shared.DTOs.PDF;
using ERPSystem.Utils.Constants.Email;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Enums;
using ERPSystem.Utils.Response;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

using static ERPSystem.Utils.Constants.General.Route;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ERPSystem.Modules.Contracts;

public class ContractsService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ContractsService> _logger;
    private readonly PdfService _pdfService;
    private readonly EmailBusinessLogic _emailBusinessLogic;
    private readonly AdditionalActService _additionalActService;

    public ContractsService(  ApplicationDbContext db,  ILogger<ContractsService> logger, EmailBusinessLogic emailBusinessLogic, PdfService pdfService, AdditionalActService additionalActService)
    {
        _db = db;
        _logger = logger;
        _emailBusinessLogic = emailBusinessLogic;
        _pdfService = pdfService;
        _additionalActService = additionalActService;
    }

    public async Task<PublicResponse> CreateAsync(CreateContractDto dto)
    {
        var response = new PublicResponse(true);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            
            if (!dto.StudentIds.Any() || !dto.CourseSessionIds.Any())
                return response.SetError(ErrorCodes.InvalidParameters, "Invalid parameters");

            if (!dto.IsUnlimited && dto.EndDate == null)
                return response.SetError(ErrorCodes.InvalidParameters, "EndDate required");

            if (!dto.IsUnlimited && dto.EndDate < dto.StartDate)
                return response.SetError(ErrorCodes.InvalidParameters, "Invalid period");
     
            var students = await _db.Students
                .Where(x => dto.StudentIds.Contains(x.Id))
                .ToListAsync();

            if (students.Count != dto.StudentIds.Count)
                return response.SetError(ErrorCodes.InvalidParameters, "Invalid students");

            var blockingStatuses = new[]
                {
                    ContractStatus.Draft,
                    ContractStatus.Finalized,
                    ContractStatus.SentToClient,
                    ContractStatus.SignedByClient,
                    ContractStatus.Active
                };
            var hasActiveContract = await _db.StudentContracts
               .AnyAsync(c =>
                  blockingStatuses.Contains(c.Status) &&
                  c.Parties.Any(p =>
                  p.StudentId.HasValue &&
                  dto.StudentIds.Contains(p.StudentId.Value)));

            if (hasActiveContract)
            {
                var existing = await _db.StudentContracts
                    .Where(c =>
                        blockingStatuses.Contains(c.Status) &&
                        c.Parties.Any(p =>
                            p.StudentId.HasValue &&
                            dto.StudentIds.Contains(p.StudentId.Value)))
                    .OrderByDescending(c => c.CreatedAtUtc)
                    .Select(c => new { c.Id })
                    .FirstAsync();

                return response.SetSuccess(new
                {
                    existingContractId = existing.Id
                });
            }

            Guardian? guardian = null;

            if (dto.GuardianId.HasValue)
            {
                guardian = await _db.Guardians
                    .FirstOrDefaultAsync(x => x.Id == dto.GuardianId.Value);

                if (guardian is null)
                    return response.SetError(ErrorCodes.InvalidParameters, "Guardian not found");
            }

            var hasMinor = students.Any(s => s.IsMinor);

            if (hasMinor)
            {
                if (guardian is null)
                    return response.SetError(ErrorCodes.InvalidParameters,
                        "Guardian required for minor student");

                var guardianLinked = await _db.StudentGuardians
                    .AnyAsync(sg =>
                        dto.StudentIds.Contains(sg.StudentId) &&
                        sg.GuardianId == guardian.Id);

                if (!guardianLinked)
                    return response.SetError(ErrorCodes.InvalidParameters,
                        "Guardian not linked to selected student");
            }

            var sessions = await _db.CourseSessions
                .Include(x => x.Course)
                .Where(x => dto.CourseSessionIds.Contains(x.Id))
                .ToListAsync();

            if (sessions.Count != dto.CourseSessionIds.Count)
                return response.SetError(ErrorCodes.InvalidParameters, "Invalid sessions");

            var contract = new StudentContract
            {
                ContractNumber = GenerateContractNumber(),
                StartDate = dto.StartDate,
                EndDate = dto.IsUnlimited ? null : dto.EndDate,
                IsUnlimited = dto.IsUnlimited,
                Installments = dto.Installments <= 0 ? 1 : dto.Installments,
                Status = ContractStatus.Draft,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var company = await _db.CompanySettings.FirstAsync();

            contract.CompanyNameSnapshot = company.Name;
            contract.CompanyAddressSnapshot = company.Address;
            contract.CompanyCuiSnapshot = company.CUI;
            contract.CompanyRegistrationSnapshot = company.RegistrationNumber;
            contract.CompanyIbanSnapshot = company.IBAN;
            contract.CompanyBankSnapshot = company.Bank;
            contract.CompanyEmailSnapshot = company.Email;
            contract.CompanyPhoneSnapshot = company.Phone;

            if (guardian != null)
            {
                contract.Parties.Add(new ContractParty
                {
                    GuardianId = guardian.Id,
                    Role = ContractPartyRole.Guardian
                });
            }

            foreach (var student in students)
            {
                contract.Parties.Add(new ContractParty
                {
                    StudentId = student.Id,
                    Role = ContractPartyRole.Student
                });
            }

            if (guardian != null)
            {
                var student = students.First();
                contract.BeneficiaryNameSnapshot = $"{guardian.FirstName} {guardian.LastName}";
                contract.BeneficiaryEmailSnapshot = guardian.Email;
                contract.BeneficiaryPhoneSnapshot = guardian.Phone;
                contract.BeneficiaryAddressSnapshot =
                    !string.IsNullOrWhiteSpace(guardian.Address)
                        ? guardian.Address
                        : student.Address;
            }
            else
            {
                var student = students.First();

                contract.BeneficiaryNameSnapshot = $"{student.FirstName} {student.LastName}";
                contract.BeneficiaryEmailSnapshot = student.Email;
                contract.BeneficiaryPhoneSnapshot = student.Phone;
                contract.BeneficiaryAddressSnapshot = student.Address;
            }

            foreach (var session in sessions)
            {
                contract.Courses.Add(new ContractCourse
                {
                    CourseSessionId = session.Id,
                    CourseNameSnapshot = session.Course.Name,
                    SessionNameSnapshot = session.Title,
                    PriceSnapshot = session.Fee,
                    FeeType = session.FeeType // 🔥 ADD
                });
            }

            if (dto.Discounts != null)
            {
                foreach (var d in dto.Discounts)
                {
                    contract.Discounts.Add(new ContractDiscount
                    {
                        Type = Enum.Parse<DiscountType>(d.Type),
                        Value = d.Value,
                        Reason = d.Reason
                    });
                }
            }

            var pricing = CalculatePricing(sessions, contract);

            contract.TotalAmount = pricing.Total;

            var isSubscription = sessions.Any(s => s.FeeType == CourseFeeType.Monthly);
            var isPackage = sessions.Any(s => s.FeeType == CourseFeeType.FixedPackage);


            contract.InstallmentsList.Clear();

            // detect
            var hasSubscription = pricing.SubscriptionMonthly > 0;
            var hasPackage = pricing.PackageTotal > 0;

            // 🔥 mutat mai sus
            var packageInstallments = dto.Installments <= 0 ? 1 : dto.Installments;

            // luni
            var months = pricing.SubscriptionMonths ?? packageInstallments;

            // setezi pe contract
            contract.Installments = packageInstallments;

            if (contract.IsUnlimited)
            {
                contract.InstallmentsList.Add(new ContractInstallment
                {
                    DueDate = contract.StartDate,
                    Amount = Math.Round(pricing.SubscriptionMonthly, 2),
                    PaidAmount = 0
                });
            }
            else
            {
                

                decimal packageInstallmentValue = 0;

                if (hasPackage)
                {
                    packageInstallmentValue =
                        Math.Floor((pricing.PackageTotal / packageInstallments) * 100) / 100;
                }

                var totalAssigned = packageInstallmentValue * packageInstallments;
                var remainder = pricing.PackageTotal - totalAssigned;

                for (int i = 0; i < months; i++)
                {
                    decimal amount = 0;

                    if (hasSubscription)
                    {
                        amount += pricing.SubscriptionMonthly;
                    }

                    if (hasPackage && i < packageInstallments)
                    {
                        var pkgAmount = packageInstallmentValue;

                        if (i == packageInstallments - 1)
                            pkgAmount += remainder;

                        amount += pkgAmount;
                    }

                    contract.InstallmentsList.Add(new ContractInstallment
                    {
                        DueDate = contract.StartDate.AddMonths(i),
                        Amount = Math.Round(amount, 2),
                        PaidAmount = 0
                    });
                }
            }

            contract.ContractBody = await GenerateContractBody(contract, guardian, students);
            contract.IsBodyCustomized = false;
            

            _db.StudentContracts.Add(contract);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            return response.SetCreated(new { contract.Id });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "CreateContract failed");
            return response.SetError(ErrorCodes.InternalServerError, "Internal error");
        }
    }

    public async Task<PublicResponse> UpdateAsync(int id, UpdateContractDto dto)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .Include(c => c.Courses)
            .Include(c => c.Discounts)
            .Include(c => c.Parties)
            .Include(c => c.InstallmentsList)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        if (contract.Status != ContractStatus.Draft)
            return response.SetError(ErrorCodes.InvalidParameters, "Only draft editable");

        if (!dto.IsUnlimited && dto.EndDate == null)
            return response.SetError(ErrorCodes.InvalidParameters, "EndDate required");

        if (!dto.IsUnlimited && dto.EndDate < dto.StartDate)
            return response.SetError(ErrorCodes.InvalidParameters, "Invalid period");

        var guardian = contract.Parties
            .FirstOrDefault(p => p.Role == ContractPartyRole.Guardian)?.Guardian;

        var studentIds = contract.Parties
            .Where(p => p.StudentId.HasValue)
            .Select(p => p.StudentId!.Value)
            .ToList();

        var students = await _db.Students
            .Where(s => studentIds.Contains(s.Id))
            .ToListAsync();

        var existingSessionIds = contract.Courses
            .Select(c => c.CourseSessionId)
            .ToList();

        var sessions = await _db.CourseSessions
            .Include(x => x.Course)
            .Where(x => existingSessionIds.Contains(x.Id))
            .ToListAsync();

        if (sessions.Count != existingSessionIds.Count)
            return response.SetError(ErrorCodes.InvalidParameters, "Invalid sessions");

        contract.StartDate = dto.StartDate;
        contract.EndDate = dto.IsUnlimited ? null : dto.EndDate;
        contract.IsUnlimited = dto.IsUnlimited;
        contract.Installments = dto.Installments <= 0 ? 1 : dto.Installments;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        contract.Discounts.Clear();

        if (dto.Discounts != null)
        {
            foreach (var d in dto.Discounts)
            {
                contract.Discounts.Add(new ContractDiscount
                {
                    Type = Enum.Parse<DiscountType>(d.Type),
                    Value = d.Value,
                    Reason = d.Reason
                });
            }
        }

        var pricing = CalculatePricing(sessions, contract);

        contract.TotalAmount = pricing.Total;

        contract.InstallmentsList.Clear();

        // detect
        var hasSubscription = pricing.SubscriptionMonthly > 0;
        var hasPackage = pricing.PackageTotal > 0;

        // 🔥 mutat mai sus
        var packageInstallments = dto.Installments <= 0 ? 1 : dto.Installments;

        // luni
        var months = pricing.SubscriptionMonths ?? packageInstallments;

        // setezi pe contract
        contract.Installments = packageInstallments;

        if (contract.IsUnlimited)
        {
            contract.InstallmentsList.Add(new ContractInstallment
            {
                DueDate = contract.StartDate,
                Amount = Math.Round(pricing.SubscriptionMonthly, 2),
                PaidAmount = 0
            });
        }
        else
        {
            

            decimal packageInstallmentValue = 0;

            if (hasPackage)
            {
                packageInstallmentValue =
                    Math.Floor((pricing.PackageTotal / packageInstallments) * 100) / 100;
            }

            var totalAssigned = packageInstallmentValue * packageInstallments;
            var remainder = pricing.PackageTotal - totalAssigned;

            for (int i = 0; i < months; i++)
            {
                decimal amount = 0;

                if (hasSubscription)
                {
                    amount += pricing.SubscriptionMonthly;
                }

                if (hasPackage && i < packageInstallments)
                {
                    var pkgAmount = packageInstallmentValue;

                    if (i == packageInstallments - 1)
                        pkgAmount += remainder;

                    amount += pkgAmount;
                }

                contract.InstallmentsList.Add(new ContractInstallment
                {
                    DueDate = contract.StartDate.AddMonths(i),
                    Amount = Math.Round(amount, 2),
                    PaidAmount = 0
                });
            }
        }

        if (!contract.IsBodyCustomized)
        {
            contract.ContractBody = await GenerateContractBody(contract, guardian, students);
        }

        await _db.SaveChangesAsync();

        return response.SetSuccess(new { contract.Id });
    }

    public async Task<PublicResponse> UpdateBodyAsync(int id, string body)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts.FindAsync(id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        if (contract.Status != ContractStatus.Draft)
            return response.SetError(ErrorCodes.InvalidParameters, "Only draft editable");

        if (string.IsNullOrWhiteSpace(body))
            return response.SetError(ErrorCodes.InvalidParameters, "Body cannot be empty");

        contract.ContractBody = body;
        contract.IsBodyCustomized = true; // 🔥 LOCK
        contract.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> ResetBodyAsync(int id)
    {
        var response = new PublicResponse(true);
        var contract = await _db.StudentContracts
            .Include(c => c.Parties)
            .Include(c => c.Courses)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract == null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        var guardian = contract.Parties
            .FirstOrDefault(p => p.Role == ContractPartyRole.Guardian)?.Guardian;
        var studentIds = contract.Parties
    .Where(p => p.StudentId != null)
    .Select(p => p.StudentId.Value)
    .ToList();

        var students = await _db.Students
            .Where(s => studentIds.Contains(s.Id))
            .ToListAsync();

       

        contract.ContractBody = await GenerateContractBody(contract, guardian, students);
        contract.IsBodyCustomized = false;

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> FinalizeAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .Include(c => c.Courses)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        if (contract.Status != ContractStatus.Draft)
            return response.SetError(ErrorCodes.InvalidParameters, "Only Draft can finalize");

        if (string.IsNullOrWhiteSpace(contract.ContractBody))
            return response.SetError(ErrorCodes.InvalidParameters, "Contract body empty");

        if (!contract.Courses.Any())
            return response.SetError(ErrorCodes.InvalidParameters, "Contract has no courses");

       
        contract.Status = ContractStatus.Finalized;
        contract.FinalizedAtUtc = DateTime.UtcNow;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        _db.ActivityLog.Add(new ActivityLog
        {
            EntityType = nameof(StudentContract),
            EntityId = contract.Id.ToString(),
            Action = "Finalize",
            Description = $"Contractul {contract.ContractNumber} a fost finalizat",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> SendToClientAsync(SigningEntityType type, int id)
    {
        var response = new PublicResponse(true);

        try
        {
            string clientName;
            string email;
            string link;
            string entityName;
            int entityId;

            var token = Guid.NewGuid().ToString();

            switch (type)
            {
                // ================= CONTRACT =================
                case SigningEntityType.Contract:
                    {
                        var contract = await _db.StudentContracts
                            .Include(c => c.Parties)
                                .ThenInclude(p => p.Guardian)
                            .Include(c => c.Parties)
                                .ThenInclude(p => p.Student)
                            .FirstOrDefaultAsync(c => c.Id == id);

                        if (contract is null)
                            return response.SetError(ErrorCodes.InvalidParameters, "Contract not found");

                        if (contract.Status != ContractStatus.Finalized &&
                            contract.Status != ContractStatus.SentToClient)
                            return response.SetError(ErrorCodes.InvalidParameters, "Invalid state");

                        var guardian = contract.Parties
                            .FirstOrDefault(p => p.Role == ContractPartyRole.Guardian)?.Guardian;

                        var student = contract.Parties
                            .FirstOrDefault(p => p.Role == ContractPartyRole.Student)?.Student;

                        if (guardian != null)
                        {
                            clientName = $"{guardian.FirstName} {guardian.LastName}";
                            email = guardian.Email;
                        }
                        else
                        {
                            clientName = $"{student.FirstName} {student.LastName}";
                            email = student.Email;
                        }

                        if (string.IsNullOrWhiteSpace(email))
                            return response.SetError(ErrorCodes.InvalidParameters, "Client email missing");

                        // TOKEN
                        _db.SigningTokens.Add(new SigningToken
                        {
                            EntityType = SigningEntityType.Contract,
                            EntityId = contract.Id,
                            Token = token,
                            CreatedAtUtc = DateTime.UtcNow,
                            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
                        });

                        contract.Status = ContractStatus.SentToClient;
                        contract.UpdatedAtUtc = DateTime.UtcNow;

                        entityName = nameof(StudentContract);
                        entityId = contract.Id;

                        // EMAIL
                        await _emailBusinessLogic.SendEmailTemplateAsync(
                            TemplateCode.CONTRACT_SIGN_REQUEST,
                            JsonConvert.SerializeObject(new
                            {
                                ClientName = clientName,
                                ContractNumber = contract.ContractNumber
                            }),
                            new List<string> { email },
                            $"http://localhost:4200/sign/{token}"
                        );

                        break;
                    }

                // ================= ACT ADITIONAL =================
                case SigningEntityType.AdditionalAct:
                    {
                        var act = await _db.ContractAdditionalAct
                            .Include(a => a.Contract)
                                .ThenInclude(c => c.Parties)
                                    .ThenInclude(p => p.Guardian)
                            .Include(a => a.Contract)
                                .ThenInclude(c => c.Parties)
                                    .ThenInclude(p => p.Student)
                            .FirstOrDefaultAsync(a => a.Id == id);

                        if (act is null)
                            return response.SetError(ErrorCodes.InvalidParameters, "Act not found");

                        if (act.Status != AdditionalActStatus.Finalized &&
                            act.Status != AdditionalActStatus.SentToClient)
                            return response.SetError(ErrorCodes.InvalidParameters, "Invalid state");

                        var guardian = act.Contract.Parties
                            .FirstOrDefault(p => p.Role == ContractPartyRole.Guardian)?.Guardian;

                        var student = act.Contract.Parties
                            .FirstOrDefault(p => p.Role == ContractPartyRole.Student)?.Student;

                        if (guardian != null)
                        {
                            clientName = $"{guardian.FirstName} {guardian.LastName}";
                            email = guardian.Email;
                        }
                        else
                        {
                            clientName = $"{student.FirstName} {student.LastName}";
                            email = student.Email;
                        }

                        if (string.IsNullOrWhiteSpace(email))
                            return response.SetError(ErrorCodes.InvalidParameters, "Client email missing");

                        // TOKEN
                        _db.SigningTokens.Add(new SigningToken
                        {
                            EntityType = SigningEntityType.AdditionalAct,
                            EntityId = act.Id,
                            Token = token,
                            CreatedAtUtc = DateTime.UtcNow,
                            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
                        });

                        act.Status = AdditionalActStatus.SentToClient;

                        entityName = nameof(ContractAdditionalAct);
                        entityId = act.Id;

                        // EMAIL
                        await _emailBusinessLogic.SendEmailTemplateAsync(
                            TemplateCode.ADDITIONAL_ACT_SIGN_REQUEST,
                            JsonConvert.SerializeObject(new
                            {
                                ClientName = clientName,
                                ActNumber = act.ActNumber
                            }),
                            new List<string> { email },
                            $"http://localhost:4200/sign/{token}"
                        );

                        break;
                    }

                default:
                    return response.SetError(ErrorCodes.InvalidParameters, "Unsupported entity");
            }

            // 🔥 LOG COMUN
            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = entityName,
                EntityId = entityId.ToString(),
                Action = "Send",
                Description = $"Document trimis către {email}",
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return response.SetSuccess(new { link = $"http://localhost:4200/sign/{token}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending document");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> SignByClientAsync(string token, string signature)
    {
        var response = new PublicResponse(true);

        if (string.IsNullOrWhiteSpace(signature))
            return response.SetError(ErrorCodes.InvalidParameters, "Signature missing");

        var signingToken = await _db.SigningTokens
            .FirstOrDefaultAsync(x => x.Token == token);

        if (signingToken is null || signingToken.IsUsed)
            return response.SetError(ErrorCodes.InvalidParameters, "Invalid token");

        if (signingToken.ExpiresAtUtc < DateTime.UtcNow)
            return response.SetError(ErrorCodes.InvalidParameters, "Token expired");

        switch (signingToken.EntityType)
        {
            // ================= CONTRACT =================
            case SigningEntityType.Contract:
                {
                    var contract = await _db.StudentContracts
                        .FirstOrDefaultAsync(c => c.Id == signingToken.EntityId);

                    if (contract == null)
                        return response.SetError(ErrorCodes.InvalidParameters, "Contract not found");

                    if (contract.Status != ContractStatus.SentToClient)
                        return response.SetError(ErrorCodes.InvalidParameters, "Invalid state");

                    contract.ClientSignature = signature;
                    contract.ClientSignedAtUtc = DateTime.UtcNow;
                    contract.Status = ContractStatus.SignedByClient;

                    _db.ActivityLog.Add(new ActivityLog
                    {
                        EntityType = nameof(StudentContract),
                        EntityId = contract.Id.ToString(),
                        Action = "SignClient",
                        Description = "Clientul a semnat contractul",
                        CreatedAtUtc = DateTime.UtcNow
                    });

                    break;
                }

            // ================= ACT ADITIONAL =================
            case SigningEntityType.AdditionalAct:
                {
                    var act = await _db.ContractAdditionalAct
                        .FirstOrDefaultAsync(a => a.Id == signingToken.EntityId);

                    if (act == null)
                        return response.SetError(ErrorCodes.InvalidParameters, "Act not found");

                    if (act.Status != AdditionalActStatus.SentToClient)
                        return response.SetError(ErrorCodes.InvalidParameters, "Invalid state");

                    act.ClientSignature = signature;
                    act.ClientSignedAtUtc = DateTime.UtcNow;
                    act.Status = AdditionalActStatus.SignedByClient;

                    _db.ActivityLog.Add(new ActivityLog
                    {
                        EntityType = nameof(ContractAdditionalAct),
                        EntityId = act.Id.ToString(),
                        Action = "SignClient",
                        Description = "Clientul a semnat actul adițional",
                        CreatedAtUtc = DateTime.UtcNow
                    });

                    break;
                }

            default:
                return response.SetError(ErrorCodes.InvalidParameters, "Unsupported entity");
        }

        signingToken.IsUsed = true;

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> GetContractForSigningAsync(string token)
    {
        var response = new PublicResponse(true);

        var signingToken = await _db.SigningTokens
            .FirstOrDefaultAsync(x => x.Token == token);

        if (signingToken == null || signingToken.IsUsed)
            return response.SetError(ErrorCodes.InvalidParameters, "Invalid token");

        switch (signingToken.EntityType)
        {
            case SigningEntityType.Contract:
                {
                    var contract = await _db.StudentContracts
                        .FirstOrDefaultAsync(c => c.Id == signingToken.EntityId);

                    if (contract == null)
                        return response.SetError(ErrorCodes.InvalidParameters, "Contract not found");

                    return response.SetSuccess(new
                    {
                        Type = "Contract",
                        contract.ContractNumber,
                        Body = contract.ContractBody,
                        contract.PdfPath
                    });
                }

            case SigningEntityType.AdditionalAct:
                {
                    var act = await _db.ContractAdditionalAct
                        .FirstOrDefaultAsync(a => a.Id == signingToken.EntityId);

                    if (act == null)
                        return response.SetError(ErrorCodes.InvalidParameters, "Act not found");

                    return response.SetSuccess(new
                    {
                        Type = "AdditionalAct",
                        act.ActNumber,
                        Body = act.Body,
                        act.PdfPath
                    });
                }

            default:
                return response.SetError(ErrorCodes.InvalidParameters, "Unsupported entity");
        }
    }

    public async Task<PublicResponse> SignByAdminAsync(SigningEntityType type, int id, string signature)
    {
        var response = new PublicResponse(true);

        if (string.IsNullOrWhiteSpace(signature))
            return response.SetError(ErrorCodes.InvalidParameters, "Signature missing");

        try
        {
            switch (type)
            {
                // ================= CONTRACT =================
                case SigningEntityType.Contract:
                    {
                        var contract = await _db.StudentContracts
                            .Include(c => c.Parties)
                            .Include(c => c.Courses)
                            .FirstOrDefaultAsync(c => c.Id == id);

                        if (contract is null)
                            return response.SetError(ErrorCodes.InvalidParameters, "Contract not found");

                        if (contract.Status != ContractStatus.SignedByClient)
                            return response.SetError(ErrorCodes.InvalidParameters, "Client must sign first");

                        contract.AdminSignature = signature;
                        contract.AdminSignedAtUtc = DateTime.UtcNow;

                        contract.Status = ContractStatus.Active;
                        contract.ActivatedAtUtc = DateTime.UtcNow;
                        contract.UpdatedAtUtc = DateTime.UtcNow;

                        // 🔥 ENROLLMENTS
                        var studentIds = contract.Parties
                            .Where(p => p.StudentId.HasValue && p.Role == ContractPartyRole.Student)
                            .Select(p => p.StudentId!.Value)
                            .ToList();

                        var sessionIds = contract.Courses
                            .Select(c => c.CourseSessionId)
                            .ToList();

                        var enrollments = await _db.CourseEnrollments
                            .Where(e =>
                                studentIds.Contains(e.StudentId) &&
                                sessionIds.Contains(e.CourseSessionId) &&
                                e.IsActive)
                            .ToListAsync();

                        foreach (var e in enrollments)
                        {
                            if (e.ContractId == null)
                                e.ContractId = contract.Id;
                        }

                        // 🔥 PDF
                        contract.PdfPath = _pdfService.GeneratePdf(new PdfDocumentModel
                        {
                            Title = "CONTRACT DE PRESTĂRI SERVICII",
                            Number = contract.ContractNumber,
                            Body = contract.ContractBody,
                            CompanyName = contract.CompanyNameSnapshot,
                            BeneficiaryName = contract.BeneficiaryNameSnapshot,
                            AdminSignature = contract.AdminSignature,
                            ClientSignature = contract.ClientSignature,
                            AdminSignedAt = contract.AdminSignedAtUtc,
                            ClientSignedAt = contract.ClientSignedAtUtc
                        }, "CTR");

                        _db.ActivityLog.Add(new ActivityLog
                        {
                            EntityType = nameof(StudentContract),
                            EntityId = contract.Id.ToString(),
                            Action = "SignAdmin",
                            Description = "Administratorul a semnat contractul",
                            CreatedAtUtc = DateTime.UtcNow
                        });

                        break;
                    }

                // ================= ACT ADITIONAL =================
                case SigningEntityType.AdditionalAct:
                    {
                        var act = await _db.ContractAdditionalAct
                            .Include(a => a.Contract)
                            .Include(a => a.Items)
                            .FirstOrDefaultAsync(a => a.Id == id);

                        if (act is null)
                            return response.SetError(ErrorCodes.InvalidParameters, "Act not found");

                        if (act.Status != AdditionalActStatus.SignedByClient)
                            return response.SetError(ErrorCodes.InvalidParameters, "Client must sign first");

                        act.AdminSignature = signature;
                        act.AdminSignedAtUtc = DateTime.UtcNow;
                        act.Status = AdditionalActStatus.Active;

                        // 🔥 APPLY LOGIC REALĂ
                        await _additionalActService.ApplyAdditionalActAsync(act.Id);

                        // 🔥 PDF
                        act.PdfPath = _pdfService.GeneratePdf(new PdfDocumentModel
                        {
                            Title = "ACT ADIȚIONAL",
                            Number = act.ActNumber,
                            Body = act.Body,
                            CompanyName = act.Contract.CompanyNameSnapshot,
                            BeneficiaryName = act.Contract.BeneficiaryNameSnapshot,
                            AdminSignature = act.AdminSignature,
                            ClientSignature = act.ClientSignature,
                            AdminSignedAt = act.AdminSignedAtUtc,
                            ClientSignedAt = act.ClientSignedAtUtc,
                        }, "ACT");

                        _db.ActivityLog.Add(new ActivityLog
                        {
                            EntityType = nameof(ContractAdditionalAct),
                            EntityId = act.Id.ToString(),
                            Action = "SignAdmin",
                            Description = "Administratorul a semnat actul adițional",
                            CreatedAtUtc = DateTime.UtcNow
                        });

                        break;
                    }

                default:
                    return response.SetError(ErrorCodes.InvalidParameters, "Unsupported entity");
            }

            await _db.SaveChangesAsync();

            return response.SetSuccess(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing document by admin");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }

    public async Task<PublicResponse> CancelAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == id);
        var acts = await _db.ContractAdditionalAct
    .Where(a => a.ContractId == contract.Id)
    .ToListAsync();

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        contract.Status = ContractStatus.Cancelled;
        contract.UpdatedAtUtc = DateTime.UtcNow;
        foreach (var act in acts)
        {
            act.Status = AdditionalActStatus.Cancelled;
        }

        var installments = await _db.ContractInstallments
    .Where(i => i.ContractId == contract.Id && !i.IsPaid)
    .ToListAsync();

        foreach (var i in installments)
        {
            i.Status = InstallmentStatus.Cancelled;
        }

        // 🔥 LOG CONTRACT
        _db.ActivityLog.Add(new ActivityLog
        {
            EntityType = nameof(StudentContract),
            EntityId = contract.Id.ToString(),
            Action = "Cancelled",
            Description = $"Contract {contract.ContractNumber} a fost anulat",
            CreatedAtUtc = DateTime.UtcNow
        });

        // 🔥 STOP ENROLLMENTS
        var enrollments = await _db.CourseEnrollments
            .Include(e => e.Student)
            .Include(e => e.Session)
            .Where(e => e.ContractId == contract.Id && e.IsActive)
            .ToListAsync();

        foreach (var e in enrollments)
        {
            e.IsActive = false;
            e.EndedAtUtc = DateTime.UtcNow;

            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = nameof(CourseEnrollment),
                EntityId = e.Id.ToString(),
                Action = "EnrollmentCancelled",
                Description =
                    $"Student {e.Student.FirstName} {e.Student.LastName} eliminat din {e.Session.Title} (contract anulat)",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> CompleteAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == id);
        var acts = await _db.ContractAdditionalAct
   .Where(a => a.ContractId == contract.Id)
   .ToListAsync();

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        if (contract.Status != ContractStatus.Active)
            return response.SetError(ErrorCodes.InvalidParameters,
                "Only active contracts can be completed");

        contract.Status = ContractStatus.Completed;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var act in acts)
        {
            act.Status = AdditionalActStatus.Completed;
        }
        var installments = await _db.ContractInstallments
    .Where(i => i.ContractId == contract.Id && !i.IsPaid)
    .ToListAsync();

        foreach (var i in installments)
        {
            if (!i.IsPaid)
                i.Status = InstallmentStatus.Cancelled;
        }

        // 🔥 LOG CONTRACT
        _db.ActivityLog.Add(new ActivityLog
        {
            EntityType = nameof(StudentContract),
            EntityId = contract.Id.ToString(),
            Action = "Completed",
            Description = $"Contract {contract.ContractNumber} finalizat",
            CreatedAtUtc = DateTime.UtcNow
        });

        // 🔥 STOP ENROLLMENTS (final natural)
        var enrollments = await _db.CourseEnrollments
            .Include(e => e.Student)
            .Include(e => e.Session)
            .Where(e => e.ContractId == contract.Id && e.IsActive)
            .ToListAsync();

        foreach (var e in enrollments)
        {
            e.IsActive = false;
            e.EndedAtUtc = contract.EndDate ?? DateTime.UtcNow;

            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = nameof(CourseEnrollment),
                EntityId = e.Id.ToString(),
                Action = "EnrollmentCompleted",
                Description =
                    $"Student {e.Student.FirstName} {e.Student.LastName} a finalizat {e.Session.Title}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    public async Task ExpireContractsAsync()
    {
        var contracts = await _db.StudentContracts
            .Where(c =>
                c.Status == ContractStatus.Active &&
                !c.IsUnlimited &&
                c.EndDate < DateTime.UtcNow)
            .ToListAsync();


        foreach (var contract in contracts)
        {
            contract.Status = ContractStatus.Expired;
            contract.UpdatedAtUtc = DateTime.UtcNow;
            var acts = await _db.ContractAdditionalAct
           .Where(a => a.ContractId == contract.Id &&
                       a.Status == AdditionalActStatus.Active)
           .ToListAsync();
            foreach (var act in acts)
            {
                act.Status = AdditionalActStatus.Expired;
            }

                // 🔥 log contract
                _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = nameof(StudentContract),
                EntityId = contract.Id.ToString(),
                Action = "Expired",
                Description = $"Contract {contract.ContractNumber} a expirat",
                CreatedAtUtc = DateTime.UtcNow
            });

            var installments = await _db.ContractInstallments
    .Where(i => i.ContractId == contract.Id && !i.IsPaid)
    .ToListAsync();

            foreach (var i in installments)
            {
                if (!i.IsPaid)
                    i.Status = InstallmentStatus.Expired;
            }

            // 🔥 enrollments pentru contract
            var enrollments = await _db.CourseEnrollments
                .Include(e => e.Student)
                .Include(e => e.Session)
                .Where(e => e.ContractId == contract.Id && e.IsActive)
                .ToListAsync();

            foreach (var e in enrollments)
            {
                e.IsActive = false;
                e.EndedAtUtc = DateTime.UtcNow;

                // 🔥 log per enrollment (FOARTE util în UI)
                _db.ActivityLog.Add(new ActivityLog
                {
                    EntityType = nameof(CourseEnrollment),
                    EntityId = e.Id.ToString(),
                    Action = "EnrollmentEnded",
                    Description =
                        $"Student {e.Student.FirstName} {e.Student.LastName} a fost scos din sesiunea {e.Session.Title} (contract expirat)",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<PublicResponse> SuspendAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        if (contract.Status != ContractStatus.Active)
            return response.SetError(ErrorCodes.InvalidParameters,
                "Only active contracts can be suspended");
        var acts = await _db.ContractAdditionalAct
    .Where(a => a.ContractId == contract.Id)
    .ToListAsync();

        contract.Status = ContractStatus.Suspended;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var act in acts)
        {
            act.Status = AdditionalActStatus.Suspended;
        }

        var installments = await _db.ContractInstallments
    .Where(i => i.ContractId == contract.Id && !i.IsPaid)
    .ToListAsync();

        foreach (var i in installments)
        {
            if (!i.IsPaid)
                i.Status = InstallmentStatus.Suspended;
        }


        // 🔥 LOG CONTRACT
        _db.ActivityLog.Add(new ActivityLog
        {
            EntityType = nameof(StudentContract),
            EntityId = contract.Id.ToString(),
            Action = "Suspended",
            Description = $"Contract {contract.ContractNumber} a fost suspendat",
            CreatedAtUtc = DateTime.UtcNow
        });

        // 🔥 PAUSE ENROLLMENTS (nu terminate!)
        var enrollments = await _db.CourseEnrollments
            .Include(e => e.Student)
            .Include(e => e.Session)
            .Where(e => e.ContractId == contract.Id && e.IsActive)
            .ToListAsync();

        foreach (var e in enrollments)
        {
            e.IsActive = false;

            // ⚠️ NU setăm EndedAtUtc → nu e final

            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = nameof(CourseEnrollment),
                EntityId = e.Id.ToString(),
                Action = "EnrollmentSuspended",
                Description =
                    $"Student {e.Student.FirstName} {e.Student.LastName} suspendat din {e.Session.Title}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> ResumeAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts.FindAsync(id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        if (contract.Status != ContractStatus.Suspended)
            return response.SetError(ErrorCodes.InvalidParameters,
                "Only suspended contracts can be resumed");
        var acts = await _db.ContractAdditionalAct
    .Where(a => a.ContractId == contract.Id)
    .ToListAsync();

        contract.Status = ContractStatus.Active;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var act in acts)
        {
            act.Status = AdditionalActStatus.Active;
        }

        var installments = await _db.ContractInstallments
   .Where(i => i.ContractId == contract.Id && !i.IsPaid)
   .ToListAsync();

        foreach (var i in installments)
        {
            if (i.Status == InstallmentStatus.Suspended)
                i.Status = InstallmentStatus.Pending;
        }

        // 🔥 REACTIVARE ENROLLMENTS
        var enrollments = await _db.CourseEnrollments
            .Include(e => e.Student)
            .Include(e => e.Session)
            .Where(e => e.ContractId == contract.Id && !e.IsActive && e.EndedAtUtc == null)
            .ToListAsync();

        foreach (var e in enrollments)
        {
            e.IsActive = true;

            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = nameof(CourseEnrollment),
                EntityId = e.Id.ToString(),
                Action = "EnrollmentResumed",
                Description = $"Student {e.Student.FirstName} {e.Student.LastName} reluat în {e.Session.Title}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        // 🔥 LOG CONTRACT
        _db.ActivityLog.Add(new ActivityLog
        {
            EntityType = nameof(StudentContract),
            EntityId = contract.Id.ToString(),
            Action = "Resumed",
            Description = $"Contract {contract.ContractNumber} a fost reluat",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    public async Task<IResult> DownloadContractAsync(int id)
    {
        var contract = await _db.StudentContracts.FindAsync(id);

        if (contract == null)
            return Results.NotFound();

        if (string.IsNullOrEmpty(contract.PdfPath))
            return Results.BadRequest("PDF not generated");

        var filePath = Path.Combine("wwwroot", "contracts", contract.PdfPath);

        if (!File.Exists(filePath))
        {
            // 🔥 regenerează dacă lipsește
            var fileName = _pdfService.GeneratePdf(new PdfDocumentModel
            {
                Title = "CONTRACT DE PRESTĂRI SERVICII",
                Number = contract.ContractNumber,
                Body = contract.ContractBody,
                CompanyName = contract.CompanyNameSnapshot,
                BeneficiaryName = contract.BeneficiaryNameSnapshot,
                AdminSignature = contract.AdminSignature,
                ClientSignature = contract.ClientSignature,
                AdminSignedAt = contract.AdminSignedAtUtc,
                ClientSignedAt = contract.ClientSignedAtUtc
            }, "CTR");

            contract.PdfPath = fileName;
            await _db.SaveChangesAsync();

            filePath = Path.Combine("wwwroot", "contracts", fileName);
        }



        var bytes = await File.ReadAllBytesAsync(filePath);

        return Results.File(
            bytes,
            "application/pdf",
            $"Contract_{contract.ContractNumber}.pdf"
        );


    }

    public async Task<PublicResponse> ListByStudentAsync(int studentId)
    {
        var response = new PublicResponse(true);

        var contracts = await _db.StudentContracts
            .AsNoTracking()
            .Include(c => c.Parties)
                .ThenInclude(p => p.Guardian)
            .Include(c => c.InstallmentsList) // 🔥 important
            .Where(c => c.Parties.Any(p =>
                p.StudentId == studentId &&
                p.Role == ContractPartyRole.Student))
            .Select(c => new ContractListItemDto
            {
                Id = c.Id,
                ContractNumber = c.ContractNumber,

                GuardianName = c.Parties
                    .Where(p => p.Role == ContractPartyRole.Guardian)
                    .Select(p => p.Guardian.FirstName + " " + p.Guardian.LastName)
                    .FirstOrDefault(),

                StartDate = c.StartDate,
                EndDate = c.EndDate,

                TotalAmount = c.TotalAmount,

                // 🔥 display smart
                DisplayTotal =
                    c.TotalAmount.HasValue
                        ? c.TotalAmount.Value.ToString("0.##") + " RON"
                        : "Abonament lunar",

                IsUnlimited = c.IsUnlimited,

                // 🔥 lunar (primul installment)
                MonthlyAmount = c.InstallmentsList
                    .OrderBy(i => i.DueDate)
                    .Select(i => i.Amount)
                    .FirstOrDefault(),

                Status = c.Status.ToString(),
                CreatedAtUtc = c.CreatedAtUtc
            })
            .ToListAsync();

        return response.SetSuccess(contracts);
    }

    public async Task<PublicResponse> GetByIdAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .AsNoTracking()
            .Include(c => c.Courses)
            .Include(c => c.Discounts)
            .Include(c => c.InstallmentsList) // 🔥 important
            .Include(c => c.Parties)
                .ThenInclude(p => p.Student)
            .Include(c => c.Parties)
                .ThenInclude(p => p.Guardian)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        var monthlyAmount = contract.TotalAmount == null
     ? contract.InstallmentsList.FirstOrDefault()?.Amount ?? 0
     : 0;

        var displayTotal = contract.TotalAmount.HasValue
    ? $"{contract.TotalAmount.Value:F2} RON"
    : $"{monthlyAmount:F2} RON / lună";

        var dto = new ContractDetailsDto(
            contract.Id,
            contract.ContractNumber,
            contract.StartDate,
            contract.EndDate,
            contract.IsUnlimited,
            contract.TotalAmount,


            displayTotal,
            monthlyAmount,

            contract.Installments,
            contract.Status.ToString(),
            contract.CreatedAtUtc,
            contract.FinalizedAtUtc,

            // semnături
            contract.ClientSignature,
            contract.ClientSignedAtUtc,
            contract.AdminSignature,
            contract.AdminSignedAtUtc,

            // company
            contract.CompanyNameSnapshot,
            contract.CompanyAddressSnapshot,
            contract.CompanyCuiSnapshot,
            contract.CompanyRegistrationSnapshot,
            contract.CompanyIbanSnapshot,
            contract.CompanyBankSnapshot,
            contract.CompanyEmailSnapshot,
            contract.CompanyPhoneSnapshot,

            // beneficiar
            contract.BeneficiaryNameSnapshot,
            contract.BeneficiaryEmailSnapshot,
            contract.BeneficiaryPhoneSnapshot,
            contract.BeneficiaryAddressSnapshot,

            contract.ContractBody,

            // parties
            contract.Parties.Select(p => new ContractPartyDto(
                p.StudentId,
                p.Student != null ? p.Student.FirstName + " " + p.Student.LastName : null,
                p.GuardianId,
                p.Guardian != null ? p.Guardian.FirstName + " " + p.Guardian.LastName : null,
                p.Role.ToString()
            )).ToList(),

            // courses
            contract.Courses.Select(c => new ContractCourseDto(
                  c.CourseSessionId,
                  c.CourseNameSnapshot,
                  c.SessionNameSnapshot,
                  c.PriceSnapshot,
                  (int)c.FeeType // 🔥 ADD
              )).ToList(),

            // discounts
            contract.Discounts.Select(d => new ContractDiscountDto(
                d.Type.ToString(),
                d.Value,
                d.Reason,
                d.Scope.ToString() // 🔥 ADD
            )).ToList(),

            // 🔥 installments
            contract.InstallmentsList
                .OrderBy(i => i.DueDate)
                .Select(i => new InstallmentDto
                {
                    DueDate = i.DueDate,
                    Amount = i.Amount,
                    PaidAmount = i.PaidAmount
                })
                .ToList()
        );

        return response.SetSuccess(dto);
    }

    private PricingResult CalculatePricing(  List<CourseSession> sessions,  StudentContract contract)
    {
        var result = new PricingResult();

        // split types
        var packageSessions = sessions
            .Where(s => s.FeeType == CourseFeeType.FixedPackage)
            .ToList();

        var subscriptionSessions = sessions
            .Where(s => s.FeeType == CourseFeeType.Monthly)
            .ToList();

        result.PackageTotal = packageSessions.Sum(s => s.Fee);

        result.SubscriptionMonthly = subscriptionSessions.Sum(s => s.Fee);

        if (!contract.IsUnlimited && subscriptionSessions.Any())
        {
            var months =
                ((contract.EndDate!.Value.Year - contract.StartDate.Year) * 12) +
                contract.EndDate.Value.Month - contract.StartDate.Month + 1;

            result.SubscriptionMonths = months;
            result.SubscriptionTotal = result.SubscriptionMonthly * months;
        }
        else
        {
            result.SubscriptionMonths = null;
            result.SubscriptionTotal = null;
        }

        foreach (var d in contract.Discounts)
        {
            switch (d.Scope)
            {
                case DiscountScope.Package:
                    result.PackageTotal = ApplyDiscount(result.PackageTotal, d);
                    break;

                case DiscountScope.Subscription:

                    if (result.SubscriptionTotal.HasValue)
                    {
                        result.SubscriptionTotal =
                            ApplyDiscount(result.SubscriptionTotal.Value, d);
                    }
                    else
                    {
                        // unlimited → aplic pe lunar
                        result.SubscriptionMonthly =
                            ApplyDiscount(result.SubscriptionMonthly, d);
                    }

                    break;

                case DiscountScope.Total:

                    var currentTotal =
                        result.PackageTotal +
                        (result.SubscriptionTotal ?? 0);

                    var discountedTotal =
                        ApplyDiscount(currentTotal, d);

                    if (currentTotal > 0)
                    {
                        var ratio = discountedTotal / currentTotal;

                        result.PackageTotal *= ratio;

                        if (result.SubscriptionTotal.HasValue)
                        {
                            result.SubscriptionTotal *= ratio;
                        }
                    }

                    break;
            }
        }

        if (result.SubscriptionTotal.HasValue)
        {
            result.Total = result.PackageTotal + result.SubscriptionTotal;
        }
        else if (subscriptionSessions.Any())
        {
            // unlimited subscription → total necunoscut
            result.Total = null;
        }
        else
        {
            result.Total = result.PackageTotal;
        }

        return result;
    }

    private decimal ApplyDiscount(decimal amount, ContractDiscount d)
    {
        decimal result = amount;

        if (d.Type == DiscountType.Percentage)
        {
            result -= amount * (d.Value / 100m);
        }
        else
        {
            result -= d.Value;
        }

        return Math.Max(0, Math.Round(result, 2));
    }

    private string GenerateContractNumber()
    {
        return $"CTR-{DateTime.UtcNow:yyyy}-{DateTime.UtcNow.Ticks.ToString()[^6..]}";
    }

    private async Task<string> GenerateContractBody( StudentContract contract, Guardian? guardian,  List<ERPSystem.Data.Entities.Student> students)
    {
        var template = await _db.ContractTemplates
            .Where(x => x.IsActive && x.Name == "Default ERP Contract Template")
            .Select(x => x.Body)
            .FirstAsync();

        var studentList = string.Join("",
            students.Select(s => $"<li>{s.FullName}</li>"));

        var coursesList = string.Join("",
            contract.Courses.Select(c =>
                $"<li>{c.CourseNameSnapshot} ({c.SessionNameSnapshot}) – {c.PriceSnapshot:F2} RON</li>"));

        var subtotal = contract.Courses.Sum(c => c.PriceSnapshot);

        var totalDisplay = contract.TotalAmount.HasValue
            ? $"{contract.TotalAmount.Value:F2} RON"
            : "Plată lunară";

        decimal discountValue = 0;

        if (contract.TotalAmount.HasValue)
        {
            discountValue = subtotal - contract.TotalAmount.Value;

            if (discountValue < 0)
                discountValue = 0;
        }

        var discountDisplay = $"{discountValue:F2} RON";

        var monthly = contract.InstallmentsList
            .OrderBy(i => i.DueDate)
            .Select(i => i.Amount)
            .FirstOrDefault();

        var monthlyDisplay = monthly > 0
            ? $"{monthly:F2} RON / lună"
            : "-";

        var period = contract.IsUnlimited
            ? "Perioadă nedeterminată"
            : $"{contract.StartDate:dd.MM.yyyy} - {contract.EndDate:dd.MM.yyyy}";

        var values = new Dictionary<string, string>
        {
            ["ContractNumber"] = contract.ContractNumber,
            ["Date"] = DateTime.UtcNow.ToString("dd.MM.yyyy"),

            ["CompanyName"] = contract.CompanyNameSnapshot,
            ["CompanyCui"] = contract.CompanyCuiSnapshot,
            ["CompanyRegistration"] = contract.CompanyRegistrationSnapshot,
            ["CompanyAddress"] = contract.CompanyAddressSnapshot,
            ["CompanyIban"] = contract.CompanyIbanSnapshot,
            ["CompanyBank"] = contract.CompanyBankSnapshot,
            ["CompanyEmail"] = contract.CompanyEmailSnapshot,
            ["CompanyPhone"] = contract.CompanyPhoneSnapshot,

            ["BeneficiaryName"] = contract.BeneficiaryNameSnapshot,
            ["BeneficiaryEmail"] = contract.BeneficiaryEmailSnapshot,
            ["BeneficiaryPhone"] = contract.BeneficiaryPhoneSnapshot,
            ["BeneficiaryAddress"] = contract.BeneficiaryAddressSnapshot,

            ["Students"] = studentList,
            ["Courses"] = coursesList,

            ["ContractPeriod"] = period,

            ["Subtotal"] = $"{subtotal:F2} RON",
            ["Discount"] = discountDisplay,
            ["Total"] = totalDisplay,

            ["MonthlyAmount"] = monthlyDisplay,
            ["Installments"] = contract.Installments.ToString()
        };

        return ApplyTemplate(template, values);
    }

    private string ApplyTemplate(string template, Dictionary<string, string> values)
    {
        foreach (var item in values)
        {
            template = template.Replace($"{{{{{item.Key}}}}}", item.Value ?? "");
        }

        return template;
    }

    public async Task GenerateMonthlyInstallments()
    {
        var today = DateTime.UtcNow.Date;

        var contracts = await _db.StudentContracts
            .Where(c => c.IsUnlimited && c.Status == ContractStatus.Active)
            .Include(c => c.InstallmentsList)
            .ToListAsync();

        foreach (var contract in contracts)
        {
            var lastInstallment = contract.InstallmentsList
                .OrderByDescending(i => i.DueDate)
                .FirstOrDefault();

            if (lastInstallment == null)
                continue;

            var nextDueDate = lastInstallment.DueDate.AddMonths(1);

            // 🔥 deja există?
            var exists = contract.InstallmentsList
                .Any(i => i.DueDate.Date == nextDueDate.Date);

            if (exists)
                continue;

            // 🔥 doar dacă a trecut data
            if (nextDueDate > today)
                continue;

            // 🔥 calculează suma (IMPORTANT)
            var amount = contract.MonthlyAmount; // sau recalcul din courses

            contract.InstallmentsList.Add(new ContractInstallment
            {
                DueDate = nextDueDate,
                Amount = amount,
                PaidAmount = 0
            });
        }

        await _db.SaveChangesAsync();
    }



}