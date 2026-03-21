using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
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

    public ContractsService(  ApplicationDbContext db,  ILogger<ContractsService> logger, EmailBusinessLogic emailBusinessLogic, PdfService pdfService)
    {
        _db = db;
        _logger = logger;
        _emailBusinessLogic = emailBusinessLogic;
        _pdfService = pdfService;
    }

    // =========================
    // CREATE CONTRACT
    // =========================
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
                // 🔥 fallback adresă
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
                    PriceSnapshot = session.Fee
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

            contract.TotalAmount = CalculateTotal(contract);
            
            if (contract.Installments > 1)
            {
                var baseAmount = Math.Floor((contract.TotalAmount / contract.Installments) * 100) / 100;

                var totalAssigned = baseAmount * contract.Installments;
                var remainder = contract.TotalAmount - totalAssigned;

                for (int i = 0; i < contract.Installments; i++)
                {
                    var amount = baseAmount;

                    if (i == contract.Installments - 1)
                    {
                        amount += remainder;
                    }

                    contract.InstallmentsList.Add(new ContractInstallment
                    {
                        DueDate = contract.StartDate.AddMonths(i),
                        Amount = amount,
                        IsPaid = false
                    });
                }
            }
            else
            {
                contract.InstallmentsList.Add(new ContractInstallment
                {
                    DueDate = contract.StartDate,
                    Amount = contract.TotalAmount,
                    IsPaid = false
                });
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

    // =========================
    // UPDATE (Draft only)
    // =========================
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

        if (!dto.CourseSessionIds.Any())
            return response.SetError(ErrorCodes.InvalidParameters, "No courses selected");

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


        var sessions = await _db.CourseSessions
            .Include(x => x.Course)
            .Where(x => dto.CourseSessionIds.Contains(x.Id))
            .ToListAsync();

        if (sessions.Count != dto.CourseSessionIds.Count)
            return response.SetError(ErrorCodes.InvalidParameters, "Invalid sessions");


        contract.StartDate = dto.StartDate;
        contract.EndDate = dto.IsUnlimited ? null : dto.EndDate;
        contract.IsUnlimited = dto.IsUnlimited;
        contract.Installments = dto.Installments <= 0 ? 1 : dto.Installments;
        contract.UpdatedAtUtc = DateTime.UtcNow;


        contract.Courses.Clear();
        contract.Discounts.Clear();


        foreach (var session in sessions)
        {
            contract.Courses.Add(new ContractCourse
            {
                CourseSessionId = session.Id,
                CourseNameSnapshot = session.Course.Name,
                SessionNameSnapshot = session.Title,
                PriceSnapshot = session.Fee
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

     
        contract.TotalAmount = CalculateTotal(contract);
  
        contract.InstallmentsList.Clear();

        if (contract.Installments > 1)
        {
            var baseAmount = Math.Floor((contract.TotalAmount / contract.Installments) * 100) / 100;
            var totalAssigned = baseAmount * contract.Installments;
            var remainder = contract.TotalAmount - totalAssigned;

            for (int i = 0; i < contract.Installments; i++)
            {
                var amount = baseAmount;

                if (i == contract.Installments - 1)
                    amount += remainder;

                contract.InstallmentsList.Add(new ContractInstallment
                {
                    DueDate = contract.StartDate.AddMonths(i),
                    Amount = amount,
                    IsPaid = false
                });
            }
        }
        else
        {
            contract.InstallmentsList.Add(new ContractInstallment
            {
                DueDate = contract.StartDate,
                Amount = contract.TotalAmount,
                IsPaid = false
            });
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

    // =========================
    // FINALIZE
    // =========================
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
            EntityId = contract.Id,
            Action = "Finalize",
            Description = $"Contractul {contract.ContractNumber} a fost finalizat",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }


    // =========================
    // SEND TO CLIENT
    // =========================
    public async Task<PublicResponse> SendToClientAsync(int id)
    {
        var response = new PublicResponse(true);

        try
        {
            var contract = await _db.StudentContracts
    .Include(c => c.Parties)
        .ThenInclude(p => p.Guardian)
    .Include(c => c.Parties)
        .ThenInclude(p => p.Student)
    .FirstOrDefaultAsync(c => c.Id == id);

            if (contract is null)
                return response.SetError(ErrorCodes.InvalidParameters, "Not found");

            if (contract.Status != ContractStatus.Finalized)
                return response.SetError(ErrorCodes.InvalidParameters, "Contract must be finalized");

            var guardian = contract.Parties
      .FirstOrDefault(p => p.Role == ContractPartyRole.Guardian)?.Guardian;

            var student = contract.Parties
                .FirstOrDefault(p => p.Role == ContractPartyRole.Student)?.Student;

            string clientName;
            string email;

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

            // generăm token pentru semnare
            var token = Guid.NewGuid().ToString();

            var signingToken = new ContractSigningToken
            {
                ContractId = contract.Id,
                Token = token,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                IsUsed = false
            };

            _db.ContractSigningTokens.Add(signingToken);

            var link = $"http://localhost:4200/sign-contract/{token}";

            var model = new ContractSignEmailModel
            {
                ClientName = clientName,
                ContractNumber = contract.ContractNumber
            };

            var to = new List<string> { email };

            var emailResponse = await _emailBusinessLogic.SendEmailTemplateAsync(
                TemplateCode.CONTRACT_SIGN_REQUEST,
                JsonConvert.SerializeObject(model),
                to,
                link
            );

            contract.Status = ContractStatus.SentToClient;
            contract.UpdatedAtUtc = DateTime.UtcNow;


            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = nameof(StudentContract),
                EntityId = contract.Id,
                Action = "Send",
                Description = $"Contract trimis către {email}",
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return response.SetSuccess(new { link });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending contract email.");
            return response.SetError(ErrorCodes.InternalServerError, ErrorMessages.InternalServerError);
        }
    }
    // =========================
    // SIGN BY CLIENT
    // =========================
    public async Task<PublicResponse> SignByClientAsync(string token, string signature)
    {
        var response = new PublicResponse(true);

        if (string.IsNullOrWhiteSpace(signature))
            return response.SetError(
                ErrorCodes.InvalidParameters,
                "Signature missing");

        var signingToken = await _db.ContractSigningTokens
            .Include(x => x.Contract)
            .FirstOrDefaultAsync(x => x.Token == token);

        if (signingToken is null || signingToken.IsUsed)
            return response.SetError(ErrorCodes.InvalidParameters, "Invalid token");

        if (signingToken.ExpiresAtUtc < DateTime.UtcNow)
            return response.SetError(ErrorCodes.InvalidParameters, "Token expired");

        var contract = signingToken.Contract;

        if (contract.Status != ContractStatus.SentToClient)
            return response.SetError(
                ErrorCodes.InvalidParameters,
                "Contract cannot be signed");

        contract.ClientSignature = signature;
        contract.ClientSignedAtUtc = DateTime.UtcNow;
        contract.Status = ContractStatus.SignedByClient;

        signingToken.IsUsed = true;

        _db.ActivityLog.Add(new ActivityLog
        {
            EntityType = nameof(StudentContract),
            EntityId = contract.Id,
            Action = "SignClient",
            Description = "Clientul a semnat contractul",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> GetContractForSigningAsync(string token)
    {
        var response = new PublicResponse(true);

        var signingToken = await _db.ContractSigningTokens
            .Include(x => x.Contract)
            .FirstOrDefaultAsync(x => x.Token == token);

        if (signingToken == null || signingToken.IsUsed)
            return response.SetError(ErrorCodes.InvalidParameters, "Invalid token");

        var contract = signingToken.Contract;

        return response.SetSuccess(new
        {
            contract.ContractNumber,
            contract.ContractBody,
            contract.PdfPath
        });
    }

    // =========================
    // SIGN by admin
    // =========================
    public async Task<PublicResponse> SignByAdminAsync(int id, string signature)
    {
        var response = new PublicResponse(true);

        if (string.IsNullOrWhiteSpace(signature))
            return response.SetError(
                ErrorCodes.InvalidParameters,
                "Signature missing");

        var contract = await _db.StudentContracts
    .Include(c => c.Parties)
    .Include(c => c.Courses)
    .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Contract not found");

        if (contract.Status != ContractStatus.SignedByClient)
            return response.SetError(
                ErrorCodes.InvalidParameters,
                "Client must sign first");

        contract.AdminSignature = signature;
        contract.AdminSignedAtUtc = DateTime.UtcNow;

        // activare automată
        contract.Status = ContractStatus.Active;
        contract.ActivatedAtUtc = DateTime.UtcNow;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        // 🔥 ia studentii din contract
        var studentIds = contract.Parties
            .Where(p => p.StudentId.HasValue && p.Role == ContractPartyRole.Student)
            .Select(p => p.StudentId!.Value)
            .ToList();

        // 🔥 ia sesiunile din contract
        var sessionIds = contract.Courses
            .Select(c => c.CourseSessionId)
            .ToList();

        // 🔥 ia enrollments existente
        var enrollments = await _db.CourseEnrollments
            .Where(e =>
                studentIds.Contains(e.StudentId) &&
                sessionIds.Contains(e.CourseSessionId) &&
                e.IsActive)
            .ToListAsync();

        // 🔥 leaga de contract
        foreach (var e in enrollments)
        {
            if (e.ContractId == null)
                e.ContractId = contract.Id;
        }

        try
        {
            // generăm PDF final cu ambele semnături
            var pdfName = _pdfService.GeneratePdf(new PdfDocumentModel
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF generation failed");
            throw;
        }
        //contract.PdfPath = pdfName;

        _db.ActivityLog.Add(new ActivityLog
        {
            EntityType = nameof(StudentContract),
            EntityId = contract.Id,
            Action = "SignAdmin",
            Description = "Administratorul a semnat contractul",
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    // =========================
    // LIST BY STUDENT
    // =========================
    public async Task<PublicResponse> ListByStudentAsync(int studentId)
    {
        var response = new PublicResponse(true);

        var contracts = await _db.StudentContracts
     .AsNoTracking()
     .Include(c => c.Parties)
         .ThenInclude(p => p.Guardian)
     .Where(c => c.Parties.Any(p =>
         p.StudentId == studentId &&
         p.Role == ContractPartyRole.Student))
     .Select(c => new ContractListItemDto(
         c.Id,
         c.ContractNumber,
         c.Parties
             .Where(p => p.Role == ContractPartyRole.Guardian)
             .Select(p => p.Guardian.FirstName + " " + p.Guardian.LastName)
             .FirstOrDefault(),
         c.StartDate,
         c.EndDate,
         c.TotalAmount,
         c.Status.ToString(),
         c.CreatedAtUtc
     ))
     .ToListAsync();

        return response.SetSuccess(contracts);
    }

    // =========================
    // GET BY ID
    // =========================
    public async Task<PublicResponse> GetByIdAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
    .AsNoTracking()
    .Include(c => c.Courses)
    .Include(c => c.Discounts)
    .Include(c => c.Parties)
        .ThenInclude(p => p.Student)
    .Include(c => c.Parties)
        .ThenInclude(p => p.Guardian)
    .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        var dto = new ContractDetailsDto(
     contract.Id,
     contract.ContractNumber,
     contract.StartDate,
     contract.EndDate,
     contract.IsUnlimited,
     contract.TotalAmount,
     contract.Installments,
     contract.Status.ToString(),
     contract.CreatedAtUtc,
     contract.FinalizedAtUtc,

     // 🔥 SEMNĂTURI
     contract.ClientSignature,
     contract.ClientSignedAtUtc,
     contract.AdminSignature,
     contract.AdminSignedAtUtc,

     // 🔥 COMPANY
     contract.CompanyNameSnapshot,
     contract.CompanyAddressSnapshot,
     contract.CompanyCuiSnapshot,
     contract.CompanyRegistrationSnapshot,
     contract.CompanyIbanSnapshot,
     contract.CompanyBankSnapshot,
     contract.CompanyEmailSnapshot,
     contract.CompanyPhoneSnapshot,

     // 🔥 BENEFICIAR
     contract.BeneficiaryNameSnapshot,
     contract.BeneficiaryEmailSnapshot,
     contract.BeneficiaryPhoneSnapshot,
     contract.BeneficiaryAddressSnapshot,

     contract.ContractBody,

   contract.Parties.Select(p => new ContractPartyDto(
    p.StudentId,
    p.Student != null ? p.Student.FirstName + " " + p.Student.LastName : null,
    p.GuardianId,
    p.Guardian != null ? p.Guardian.FirstName + " " + p.Guardian.LastName : null,
    p.Role.ToString()
)).ToList(),

     contract.Courses.Select(c => new ContractCourseDto(
         c.CourseSessionId,
         c.CourseNameSnapshot,
         c.SessionNameSnapshot,
         c.PriceSnapshot
     )).ToList(),

     contract.Discounts.Select(d => new ContractDiscountDto(
         d.Type.ToString(),
         d.Value,
         d.Reason
     )).ToList()
 );

        return response.SetSuccess(dto);
    }

    // =========================
    // HELPERS
    // =========================
    private decimal CalculateTotal(StudentContract contract)
    {
        var total = contract.Courses.Sum(c => c.PriceSnapshot);

        foreach (var discount in contract.Discounts)
        {
            if (discount.Type == DiscountType.Percentage)
                total -= total * (discount.Value / 100m);
            else
                total -= discount.Value;
        }

        return total < 0 ? 0 : total;
    }

    private string GenerateContractNumber()
    {
        return $"CTR-{DateTime.UtcNow:yyyy}-{DateTime.UtcNow.Ticks.ToString()[^6..]}";
    }

    private async Task<string> GenerateContractBody( StudentContract contract,    Guardian? guardian,    List<ERPSystem.Data.Entities.Student> students)
    {
        var template = await _db.ContractTemplates
            .Where(x => x.IsActive)
            .Select(x => x.Body)
            .FirstAsync();

        

        var studentList = string.Join(", ", students.Select(s => s.FullName));

        var coursesList = string.Join("\n",
            contract.Courses.Select(c =>
                $"- {c.CourseNameSnapshot} ({c.SessionNameSnapshot}) – {c.PriceSnapshot} RON"));

        string beneficiaryName;
        string beneficiaryEmail;
        string beneficiaryPhone;
        string beneficiaryAddress;

        
        var subtotal = contract.Courses.Sum(c => c.PriceSnapshot);
        var discount = subtotal - contract.TotalAmount;

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
            ["Discount"] = $"{discount:F2} RON",
            ["Total"] = $"{contract.TotalAmount:F2} RON"
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

    public async Task<PublicResponse> CancelAsync(int id)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        contract.Status = ContractStatus.Cancelled;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        // 🔥 LOG CONTRACT
        _db.ActivityLog.Add(new ActivityLog
        {
            EntityType = nameof(StudentContract),
            EntityId = contract.Id,
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
                EntityId = e.Id,
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

        if (contract is null)
            return response.SetError(ErrorCodes.InvalidParameters, "Not found");

        if (contract.Status != ContractStatus.Active)
            return response.SetError(ErrorCodes.InvalidParameters,
                "Only active contracts can be completed");

        contract.Status = ContractStatus.Completed;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        // 🔥 LOG CONTRACT
        _db.ActivityLog.Add(new ActivityLog
        {
            EntityType = nameof(StudentContract),
            EntityId = contract.Id,
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
                EntityId = e.Id,
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

            // 🔥 log contract
            _db.ActivityLog.Add(new ActivityLog
            {
                EntityType = nameof(StudentContract),
                EntityId = contract.Id,
                Action = "Expired",
                Description = $"Contract {contract.ContractNumber} a expirat",
                CreatedAtUtc = DateTime.UtcNow
            });

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
                    EntityId = e.Id,
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

        contract.Status = ContractStatus.Suspended;
        contract.UpdatedAtUtc = DateTime.UtcNow;

        // 🔥 LOG CONTRACT
        _db.ActivityLog.Add(new ActivityLog
        {
            EntityType = nameof(StudentContract),
            EntityId = contract.Id,
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
                EntityId = e.Id,
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

        contract.Status = ContractStatus.Active;
        contract.UpdatedAtUtc = DateTime.UtcNow;

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
                EntityId = e.Id,
                Action = "EnrollmentResumed",
                Description = $"Student {e.Student.FirstName} {e.Student.LastName} reluat în {e.Session.Title}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        // 🔥 LOG CONTRACT
        _db.ActivityLog.Add(new ActivityLog
        {
            EntityType = nameof(StudentContract),
            EntityId = contract.Id,
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


    public async Task<PublicResponse> GetLatestByStudentAsync(int studentId)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .Include(c => c.Parties)
            .Where(c => c.Parties.Any(p =>
                p.Role == ContractPartyRole.Student &&
                p.StudentId.HasValue &&
                p.StudentId.Value == studentId))
            .OrderByDescending(c => c.CreatedAtUtc) 
            .Select(c => new
            {
                c.Id,
                c.ContractNumber,
                c.StartDate,
                c.EndDate,
                Status = c.Status.ToString()

            })
            .FirstOrDefaultAsync();

        return response.SetSuccess(contract);
    }


    //ADITIONAL ACT
    public async Task<PublicResponse> CreateAdditionalActAsync(int contractId, CreateAdditionalActDto dto)
    {
        var response = new PublicResponse(true);

        var contract = await _db.StudentContracts
            .Include(c => c.Courses)
            .FirstOrDefaultAsync(c => c.Id == contractId);

        if (contract == null)
            return response.SetError(ErrorCodes.InvalidParameters, "Contract not found");

        if (contract.Status != ContractStatus.Active)
            return response.SetError(ErrorCodes.InvalidParameters, "Only active contracts can have additional acts");

       
        var act = new ContractAdditionalAct
        {
            ContractId = contract.Id,
            ActNumber = GenerateActNumber(),
            Type = dto.Type,
            Description = dto.Description,
            Status = "Draft",
            CreatedAtUtc = DateTime.UtcNow
        };

        if (dto.Type == AdditionalActType.AddCourse)
        {
            var sessionId = dto.CourseSessionIds.First();

            // găsești enrollment existent
            var enrollment = await _db.CourseEnrollments
                .FirstOrDefaultAsync(e =>
                    e.CourseSessionId == sessionId &&
                    e.StudentId == contract.Parties
                        .Where(p => p.StudentId != null)
                        .Select(p => p.StudentId.Value)
                        .First() &&
                    e.IsActive);

            if (enrollment == null)
                return response.SetError(ErrorCodes.InvalidParameters, "Enrollment not found");

            // 🔥 LEGI DE CONTRACT
            enrollment.ContractId = contract.Id;

            var session = await _db.CourseSessions
                .Include(x => x.Course)
                .FirstAsync(x => x.Id == sessionId);

            act.Description = $"Adăugat curs: {session.Course.Name} ({session.Title})";
        }

        if (dto.Type == AdditionalActType.RemoveCourse && dto.CourseSessionIds?.Any() == true)
        {
            var sessionId = dto.CourseSessionIds.First();

            var enrollment = await _db.CourseEnrollments
                .Include(e => e.Session)
                    .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(e =>
                    e.CourseSessionId == sessionId &&
                    e.ContractId == contract.Id);

            if (enrollment == null)
                return response.SetError(ErrorCodes.InvalidParameters, "Enrollment not found");

            // 🔥 VALIDARE IMPORTANTĂ
            if (enrollment.IsActive)
                return response.SetError(ErrorCodes.InvalidParameters,
                    "Cursul trebuie scos înainte de act");

            act.Description = $"Eliminat curs: {enrollment.Session.Course.Name} ({enrollment.Session.Title})";
        }


        _db.ContractAdditionalAct.Add(act);
        await _db.SaveChangesAsync();

        return response.SetSuccess(new { act.Id });
    }

    private string GenerateActNumber()
    {
        return $"AA-{DateTime.UtcNow:yyyy}-{DateTime.UtcNow.Ticks.ToString()[^5..]}";
    }

    public async Task<PublicResponse> FinalizeAdditionalActAsync(int id)
    {
        var response = new PublicResponse(true);

        var act = await _db.ContractAdditionalAct
            .Include(a => a.Contract)
                .ThenInclude(c => c.Courses)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (act == null)
            return response.SetError(ErrorCodes.InvalidParameters, "Act not found");

        if (act.Status != "Draft")
            return response.SetError(ErrorCodes.InvalidParameters, "Already finalized");

        var contract = act.Contract;

        switch (act.Type)
        {
            case AdditionalActType.AddCourse:
                // TODO: adaugi curs (ex din DTO salvat separat)
                break;

            case AdditionalActType.RemoveCourse:
                // dezactivezi enrollment
                break;

            case AdditionalActType.ExtendPeriod:
                contract.EndDate = contract.EndDate?.AddMonths(1);
                break;

            case AdditionalActType.ChangePrice:
                contract.TotalAmount += act.PriceDifference ?? 0;
                break;
        }

        contract.UpdatedAtUtc = DateTime.UtcNow;

        act.Status = "Finalized";

        await _db.SaveChangesAsync();

        return response.SetSuccess(true);
    }

    public async Task<PublicResponse> ListAdditionalActsAsync(int contractId)
    {
        var response = new PublicResponse(true);

        var acts = await _db.ContractAdditionalAct
            .Where(a => a.ContractId == contractId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new AdditionalActDto(
                a.Id,
                a.ActNumber,
                a.Type.ToString(),
                a.Description,
                a.Status,
                a.CreatedAtUtc
            ))
            .ToListAsync();

        return response.SetSuccess(acts);
    }



}