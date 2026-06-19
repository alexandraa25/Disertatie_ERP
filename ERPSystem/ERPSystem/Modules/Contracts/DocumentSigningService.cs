using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.AdditionalAct;
using ERPSystem.Shared.BusinessLogic;
using ERPSystem.Shared.DTOs.PDF;
using ERPSystem.Shared.Notifications;
using ERPSystem.Utils.Constants.Email;
using ERPSystem.Utils.Constants.Error;
using ERPSystem.Utils.Enums;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ERPSystem.Modules.Contracts;

public class DocumentSigningService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<DocumentSigningService> _logger;
    private readonly PdfService _pdfService;
    private readonly EmailBusinessLogic _emailBusinessLogic;
    private readonly AdditionalActService _additionalActService;
    private readonly NotificationsService _notificationsService;
    private readonly ActivityLogService _activityLogService;
    private readonly ContractRecipientResolver _recipientResolver;

    public DocumentSigningService(
        ApplicationDbContext db,
        ILogger<DocumentSigningService> logger,
        PdfService pdfService,
        EmailBusinessLogic emailBusinessLogic,
        AdditionalActService additionalActService,
        NotificationsService notificationsService,
        ActivityLogService activityLogService,
        ContractRecipientResolver recipientResolver)
    {
        _db = db;
        _logger = logger;
        _pdfService = pdfService;
        _emailBusinessLogic = emailBusinessLogic;
        _additionalActService = additionalActService;
        _notificationsService = notificationsService;
        _activityLogService = activityLogService;
        _recipientResolver = recipientResolver;
    }

    public async Task<PublicResponse> SendToClientAsync(SigningEntityType type, int id)
    {
        var response = new PublicResponse(true);

        try
        {
            var now = DateTime.UtcNow;
            var token = Guid.NewGuid().ToString();
            var signLink = $"http://localhost:4200/sign/{token}";

            string clientName;
            string email;
            string entityName;
            int entityId;
            string templateCode;
            string templatePayload;
            string notificationTitle;
            string notificationMessage;
            string notificationLink;

            switch (type)
            {
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

                        var recipient = _recipientResolver.Resolve(contract.Parties);

                        if (recipient is null || string.IsNullOrWhiteSpace(recipient.Email))
                            return response.SetError(ErrorCodes.InvalidParameters, "Client email missing");

                        clientName = recipient.Name;
                        email = recipient.Email;

                        contract.Status = ContractStatus.SentToClient;
                        contract.UpdatedAtUtc = now;

                        entityName = nameof(StudentContract);
                        entityId = contract.Id;

                        templateCode = TemplateCode.CONTRACT_SIGN_REQUEST;
                        templatePayload = JsonConvert.SerializeObject(new
                        {
                            ClientName = clientName,
                            ContractNumber = contract.ContractNumber
                        });

                        notificationTitle = "Contract trimis către client";
                        notificationMessage = $"Contractul {contract.ContractNumber} a fost trimis către {email}.";
                        notificationLink = $"/contracts/{contract.Id}";

                        break;
                    }

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

                        var recipient = _recipientResolver.Resolve(act.Contract.Parties);

                        if (recipient is null || string.IsNullOrWhiteSpace(recipient.Email))
                            return response.SetError(ErrorCodes.InvalidParameters, "Client email missing");

                        clientName = recipient.Name;
                        email = recipient.Email;

                        act.Status = AdditionalActStatus.SentToClient;

                        entityName = nameof(ContractAdditionalAct);
                        entityId = act.Id;

                        templateCode = TemplateCode.ADDITIONAL_ACT_SIGN_REQUEST;
                        templatePayload = JsonConvert.SerializeObject(new
                        {
                            ClientName = clientName,
                            ActNumber = act.ActNumber
                        });

                        notificationTitle = "Act adițional trimis către client";
                        notificationMessage = $"Actul adițional {act.ActNumber} a fost trimis către {email}.";
                        notificationLink = $"/contracts/{act.ContractId}/additional-acts/{act.Id}";

                        break;
                    }

                default:
                    return response.SetError(ErrorCodes.InvalidParameters, "Unsupported entity");
            }

            _db.SigningTokens.Add(new SigningToken
            {
                EntityType = type,
                EntityId = entityId,
                Token = token,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddDays(7)
            });

            _activityLogService.Add(
                entityName,
                entityId.ToString(),
                "Send",
                $"Document trimis către {email}"
            );

            await _db.SaveChangesAsync();

            await _emailBusinessLogic.SendEmailTemplateAsync(
                templateCode,
                templatePayload,
                new List<string> { email },
                signLink
            );

            await NotifyAdminsAsync(
                NotificationEvents.ContractActivity,
                notificationTitle,
                notificationMessage,
                "Info",
                notificationLink,
                entityName,
                entityId.ToString()
            );

            return response.SetSuccess(new { link = signLink });
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

        if (signingToken is null)
            return response.SetError(ErrorCodes.InvalidParameters, "INVALID_TOKEN");

        if (signingToken.IsUsed)
            return response.SetError(ErrorCodes.InvalidParameters, "ALREADY_SIGNED");

        if (signingToken.ExpiresAtUtc < DateTime.UtcNow)
            return response.SetError(ErrorCodes.InvalidParameters, "TOKEN_EXPIRED");

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

                    _activityLogService.Add(
                         nameof(StudentContract),
                         contract.Id.ToString(),
                         "SignClient",
                         $"Clientul a semnat contractul {contract.ContractNumber}"
                     );


                    await NotifyAdminsAsync(
                         NotificationEvents.ContractActivity,
                         "Contract semnat de client",
                         $"Clientul a semnat contractul {contract.ContractNumber}.",
                         "Success",
                         $"/contracts/{contract.Id}",
                         nameof(StudentContract),
                         contract.Id.ToString()
                     );

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

                    _activityLogService.Add(
                        nameof(ContractAdditionalAct),
                        act.Id.ToString(),
                        "SignByClient",
                        $"Clientul a semnat actul adițional"
                    );

                    await NotifyAdminsAsync(
                        NotificationEvents.ContractActivity,
                        "Act adițional semnat de client",
                        $"Clientul a semnat actul adițional {act.ActNumber}.",
                        "Success",
                        $"/contracts/{act.ContractId}/additional-acts/{act.Id}",
                        nameof(ContractAdditionalAct),
                        act.Id.ToString()
                    );

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

        if (signingToken == null)
            return response.SetError(ErrorCodes.InvalidParameters, "INVALID_TOKEN");

        if (signingToken.IsUsed)
            return response.SetError(ErrorCodes.InvalidParameters, "ALREADY_SIGNED");

        if (signingToken.ExpiresAtUtc < DateTime.UtcNow)
            return response.SetError(ErrorCodes.InvalidParameters, "TOKEN_EXPIRED");

        switch (signingToken.EntityType)
        {
            case SigningEntityType.Contract:
                {
                    var contract = await _db.StudentContracts
                        .FirstOrDefaultAsync(c => c.Id == signingToken.EntityId);

                    if (contract == null)
                        return response.SetError(ErrorCodes.InvalidParameters, "CONTRACT_NOT_FOUND");

                    if (contract.Status == ContractStatus.SignedByClient)
                        return response.SetError(ErrorCodes.InvalidParameters, "ALREADY_SIGNED");

                    if (contract.Status != ContractStatus.SentToClient)
                        return response.SetError(ErrorCodes.InvalidParameters, "INVALID_STATE");

                    return response.SetSuccess(new
                    {
                        Type = "contract",
                        contract.ContractNumber,
                        Body = contract.ContractBody,
                        contract.PdfPath
                    });
                }

            case SigningEntityType.AdditionalAct:
                {
                    var act = await _db.ContractAdditionalAct
                        .Include(a => a.Contract)
                        .FirstOrDefaultAsync(a => a.Id == signingToken.EntityId);

                    if (act == null)
                        return response.SetError(ErrorCodes.InvalidParameters, "ACT_NOT_FOUND");

                    if (act.Status == AdditionalActStatus.SignedByClient)
                        return response.SetError(ErrorCodes.InvalidParameters, "ALREADY_SIGNED");

                    if (act.Status != AdditionalActStatus.SentToClient)
                        return response.SetError(ErrorCodes.InvalidParameters, "INVALID_STATE");

                    return response.SetSuccess(new
                    {
                        Type = "act",
                        act.ActNumber,
                        ContractNumber = act.Contract.ContractNumber,
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

                        contract.PdfPath = _pdfService.GenerateContractPdf(contract);


                        _activityLogService.Add(
                            nameof(StudentContract),
                            contract.Id.ToString(),
                            "SignAdmin",
                            $"Administratorul a semnat contractul {contract.ContractNumber}"
                        );


                        await NotifyAdminsAsync(
                            NotificationEvents.ContractActivity,
                            "Contract activat",
                            $"Contractul {contract.ContractNumber} a fost semnat de administrator și activat.",
                            "Success",
                            $"/contracts/{contract.Id}",
                            nameof(StudentContract),
                            contract.Id.ToString()
                        );

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
                        

                        await _additionalActService.ApplyAdditionalActAsync(act.Id, saveChanges: false);

                        act.PdfPath = _pdfService.GenerateAdditionalActPdf(act);

                        _activityLogService.Add(
                            nameof(ContractAdditionalAct),
                            act.Id.ToString(),
                            "SignAdmin",
                            $"Administratorul a semnat actul adițional {act.ActNumber}"
                        );

                        await NotifyAdminsAsync(
                            NotificationEvents.ContractActivity,
                            "Act adițional activat",
                            $"Actul adițional {act.ActNumber} a fost semnat de administrator și activat.",
                            "Success",
                            $"/contracts/{act.ContractId}/additional-acts/{act.Id}",
                            nameof(ContractAdditionalAct),
                            act.Id.ToString()
                        );

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

    private async Task NotifyAdminsAsync( string eventType, string title, string message, string type, string link,  string entityType, string entityId)
    {
        await _notificationsService.CreateNotificationForRolesAsync(
            roleNames: new[] { "Admin", "Secretary" },
            eventType: eventType,
            title: title,
            message: message,
            type: type,
            link: link,
            entityType: entityType,
            entityId: entityId
        );
    }
}