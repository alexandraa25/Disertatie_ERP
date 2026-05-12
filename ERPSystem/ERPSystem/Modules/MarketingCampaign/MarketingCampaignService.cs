using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.MarketingCampaign.Models;
using ERPSystem.Shared.BusinessLogic;
using ERPSystem.Shared.Notifications;
using ERPSystem.Utils.Constants.Email;
using ERPSystem.Utils.Enums;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;

public class MarketingCampaignService 
{
    private readonly ApplicationDbContext _context;
    private readonly EmailBusinessLogic _emailBusinessLogic;
    private readonly ActivityLogService _activityLogService;
    private readonly NotificationsService _notificationsService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MarketingCampaignService(
     ApplicationDbContext context,
     EmailBusinessLogic emailBusinessLogic,
     ActivityLogService activityLogService,
     NotificationsService notificationsService,
     IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _emailBusinessLogic = emailBusinessLogic;
        _activityLogService = activityLogService;
        _notificationsService = notificationsService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PublicResponse> GetAllAsync(MarketingCampaignQuery query)

    {
        await DeactivateExpiredCampaignsAsync();

        if (query.Page <= 0) query.Page = 1;
        if (query.PageSize <= 0) query.PageSize = 10;

        var campaigns = _context.MarketingCampaigns
            .AsNoTracking()
            .Include(x => x.CourseSessions)
                .ThenInclude(x => x.CourseSession)
                    .ThenInclude(x => x.Course)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            campaigns = campaigns.Where(x =>
                x.Name.Contains(search) ||
                (x.Description != null && x.Description.Contains(search)));
        }

        if (query.IsActive.HasValue)
            campaigns = campaigns.Where(x => x.IsActive == query.IsActive.Value);

        if (query.Scope.HasValue)
            campaigns = campaigns.Where(x => x.DiscountScope == query.Scope.Value);

        var today = DateTime.Today;

        if (!string.IsNullOrWhiteSpace(query.PeriodStatus))
        {
            campaigns = query.PeriodStatus.ToLower() switch
            {
                "active" => campaigns.Where(x =>
                    x.IsActive &&
                    x.StartDate <= today &&
                    x.EndDate >= today),

                "expired" => campaigns.Where(x =>
                    x.EndDate < today),

                "scheduled" => campaigns.Where(x =>
                    x.IsActive &&
                    x.StartDate > today),

                _ => campaigns
            };
        }

        campaigns = query.SortBy?.ToLower() switch
        {
            "name" => query.Desc ? campaigns.OrderByDescending(x => x.Name) : campaigns.OrderBy(x => x.Name),
            "enddate" => query.Desc ? campaigns.OrderByDescending(x => x.EndDate) : campaigns.OrderBy(x => x.EndDate),
            _ => query.Desc ? campaigns.OrderByDescending(x => x.StartDate) : campaigns.OrderBy(x => x.StartDate)
        };

        var total = await campaigns.CountAsync();

        var items = await campaigns
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.StartDate,
                c.EndDate,
                c.IsActive,
                c.DiscountType,
                c.DiscountValue,
                c.DiscountScope,
                Status = !c.IsActive
                  ? "Inactive"
                  : c.StartDate > DateTime.Today
                      ? "Scheduled"
                      : c.EndDate < DateTime.Today
                          ? "Expired"
                          : "Active",
                
                UsedInContractsCount = c.ContractDiscounts.Count(),
                CourseSessions = c.CourseSessions.Select(cs => new
                {
                    cs.CourseSessionId,
                    Title = cs.CourseSession.Title,
                    CourseId = cs.CourseSession.CourseId,
                    CourseName = cs.CourseSession.Course.Name
                }).ToList()
            })
            .ToListAsync();

        return new PublicResponse(true).SetSuccess(new
        {
            Items = items,
            TotalCount = total
        });
    }

    public async Task<PublicResponse> GetByIdAsync(int id)
    {
        var campaign = await _context.MarketingCampaigns
            .Include(x => x.CourseSessions)
                .ThenInclude(x => x.CourseSession)
                    .ThenInclude(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (campaign == null)
            return new PublicResponse(false)
                .BadRequest("Campania nu a fost găsită.", "CampaignNotFound");

        return new PublicResponse(true).SetSuccess(campaign);
    }

    public async Task<PublicResponse> CreateAsync(MarketingCampaignDto dto)
    {
        var validation = ValidateCampaign(dto);

        if (!validation.IsSuccess)
            return validation;

        var campaign = new MarketingCampaign
        {
            Name = dto.Name,
            Description = dto.Description,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = dto.IsActive,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            DiscountScope = dto.DiscountScope,
            CourseSessions = dto.CourseSessionIds
                .Distinct()
                .Select(id => new MarketingCampaignCourseSessions
                {
                    CourseSessionId = id
                })
                .ToList()
        };

        _context.MarketingCampaigns.Add(campaign);
        await _context.SaveChangesAsync();

        _activityLogService.Add(
            nameof(MarketingCampaign),
            campaign.Id.ToString(),
            "Create",
            $"Campania de marketing '{campaign.Name}' a fost creată.",
            GetCurrentUser()
        );

        await _context.SaveChangesAsync();

        await _notificationsService.CreateNotificationForRolesAsync(
             roleNames: new[] { "Admin", "Manager", "Marketing" },
             eventType: NotificationEvents.MarketingActivity,
             title: "Campanie nouă",
             message: $"Campania '{campaign.Name}' a fost creată.",
             type: "Success",
             link: "/mk-campaign",
             entityType: nameof(MarketingCampaign),
             entityId: campaign.Id.ToString()
         );

        return new PublicResponse(true)
            .SetCreated(new
            {
                campaign.Id,
                campaign.Name
            });
    }

    public async Task<PublicResponse> UpdateAsync(int id, MarketingCampaignDto dto)
    {
        var validation = ValidateCampaign(dto);

        if (!validation.IsSuccess)
            return validation;

        var campaign = await _context.MarketingCampaigns
            .Include(x => x.CourseSessions)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (campaign == null)
            return new PublicResponse(false)
                .BadRequest("Campania nu a fost găsită.", "CampaignNotFound");

        campaign.Name = dto.Name;
        campaign.Description = dto.Description;
        campaign.StartDate = dto.StartDate;
        campaign.EndDate = dto.EndDate;
        campaign.IsActive = dto.IsActive;
        campaign.DiscountType = dto.DiscountType;
        campaign.DiscountValue = dto.DiscountValue;
        campaign.DiscountScope = dto.DiscountScope;

        _context.MarketingCampaignCourseSessions.RemoveRange(campaign.CourseSessions);

        campaign.CourseSessions = dto.CourseSessionIds
            .Distinct()
            .Select(sessionId => new MarketingCampaignCourseSessions
            {
                MarketingCampaignId = campaign.Id,
                CourseSessionId = sessionId
            })
            .ToList();

        await _context.SaveChangesAsync();

        _activityLogService.Add(
            nameof(MarketingCampaign),
            campaign.Id.ToString(),
            "Update",
            $"Campania de marketing '{campaign.Name}' a fost actualizată.",
            GetCurrentUser()
        );

        await _context.SaveChangesAsync();

        return new PublicResponse(true)
            .SetSuccess(new
            {
                campaign.Id,
                campaign.Name
            });
    }

    public async Task<PublicResponse> DeleteAsync(int id)
    {
        var campaign = await _context.MarketingCampaigns
            .FirstOrDefaultAsync(x => x.Id == id);

        if (campaign == null)
            return new PublicResponse(false)
                .BadRequest("Campania nu a fost găsită.", "CampaignNotFound");

        var isUsedInContracts = await _context.ContractDiscounts
            .AnyAsync(x => x.MarketingCampaignId == id);

        if (isUsedInContracts)
            return new PublicResponse(false)
                .BadRequest(
                    "Campania nu poate fi ștearsă deoarece este asociată unui contract.",
                    "CampaignUsedInContracts"
                );

        _context.MarketingCampaigns.Remove(campaign);
        await _context.SaveChangesAsync();

        _activityLogService.Add(
            nameof(MarketingCampaign),
            campaign.Id.ToString(),
            "Delete",
            $"Campania de marketing '{campaign.Name}' a fost ștearsă.",
            GetCurrentUser()

        );

        await _context.SaveChangesAsync();

        await _notificationsService.CreateNotificationForRolesAsync(
            roleNames: new[] { "Admin", "Manager", "Marketing" },
            eventType: NotificationEvents.MarketingActivity,
            title: "Campanie ștearsă",
            message: $"Campania '{campaign.Name}' a fost ștearsă.",
            type: "Warning",
            link: "/mk-campaign",
            entityType: nameof(MarketingCampaign),
            entityId: campaign.Id.ToString()
        );

        return new PublicResponse(true)
            .SetSuccess("Campania a fost ștearsă cu succes.");
    }

    public async Task<PublicResponse> ToggleActiveAsync(int id, DateTime? endDate)
    {
        var campaign = await _context.MarketingCampaigns.FindAsync(id);

        if (campaign == null)
            return new PublicResponse(false)
                .BadRequest("Campania nu a fost găsită.", "CampaignNotFound");

        if (!campaign.IsActive)
        {
            if (!endDate.HasValue)
                return new PublicResponse(false)
                    .BadRequest("Data de final este obligatorie.", "EndDateRequired");

            if (endDate.Value < DateTime.Today)
                return new PublicResponse(false)
                    .BadRequest("Data de final trebuie să fie în viitor.", "InvalidEndDate");

            campaign.EndDate = endDate.Value;
            campaign.IsActive = true;
        }
        else
        {
            campaign.IsActive = false;
        }

        await _context.SaveChangesAsync();

        var action = campaign.IsActive ? "Activate" : "Deactivate";

        _activityLogService.Add(
            nameof(MarketingCampaign),
            campaign.Id.ToString(),
            action,
            campaign.IsActive
                ? $"Campania de marketing '{campaign.Name}' a fost activată."
                : $"Campania de marketing '{campaign.Name}' a fost dezactivată.",
            GetCurrentUser()
        );

        await _context.SaveChangesAsync();

        await _notificationsService.CreateNotificationForRolesAsync(
            roleNames: new[] { "Admin", "Manager", "Marketing" },
            eventType: NotificationEvents.MarketingActivity,
            title: campaign.IsActive
                ? "Campanie activată"
                : "Campanie dezactivată",
            message: campaign.IsActive
                ? $"Campania '{campaign.Name}' a fost activată."
                : $"Campania '{campaign.Name}' a fost dezactivată.",
            type: campaign.IsActive ? "Success" : "Warning",
            link: "/mk-campaign",
            entityType: nameof(MarketingCampaign),
            entityId: campaign.Id.ToString()
        );


        return new PublicResponse(true).SetSuccess();
    }

    private static PublicResponse ValidateCampaign(MarketingCampaignDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new PublicResponse(false)
                .BadRequest("Numele campaniei este obligatoriu.", "CampaignNameRequired");

        if (dto.Name.Length > 150)
            return new PublicResponse(false)
                .BadRequest("Numele campaniei nu poate depăși 150 de caractere.", "CampaignNameTooLong");

        if (dto.StartDate == default)
            return new PublicResponse(false)
                .BadRequest("Data de început este obligatorie.", "StartDateRequired");

        if (dto.EndDate == default)
            return new PublicResponse(false)
                .BadRequest("Data de final este obligatorie.", "EndDateRequired");

        if (dto.EndDate < dto.StartDate)
            return new PublicResponse(false)
                .BadRequest("Data de final nu poate fi înainte de data de început.", "InvalidCampaignPeriod");

        if (dto.DiscountValue <= 0)
            return new PublicResponse(false)
                .BadRequest("Valoarea discountului trebuie să fie mai mare decât 0.", "InvalidDiscountValue");

        if (dto.DiscountType == DiscountType.Percentage && dto.DiscountValue > 100)
            return new PublicResponse(false)
                .BadRequest("Discountul procentual nu poate depăși 100%.", "InvalidPercentageDiscount");

        if (dto.CourseSessionIds == null || !dto.CourseSessionIds.Any())
            return new PublicResponse(false)
                .BadRequest("Campania trebuie legată de cel puțin o sesiune de curs.", "CampaignSessionsRequired");

        return new PublicResponse(true).SetSuccess();
    }

    public async Task<PublicResponse> GetAvailableCampaignsAsync( List<int> courseSessionIds)
    {
        await DeactivateExpiredCampaignsAsync();

        var today = DateTime.Today;

        var campaigns = await _context.MarketingCampaigns
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => x.StartDate <= today && x.EndDate >= today)
            .Where(x => x.CourseSessions.Any(cs =>
                courseSessionIds.Contains(cs.CourseSessionId)))
            .OrderBy(x => x.EndDate)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.DiscountType,
                x.DiscountValue,
                x.DiscountScope,
                x.StartDate,
                x.EndDate
            })
            .ToListAsync();

        return new PublicResponse(true).SetSuccess(campaigns);
    }

    private async Task DeactivateExpiredCampaignsAsync()
    {
        var today = DateTime.Today;

        var expiredCampaigns = await _context.MarketingCampaigns
            .Where(x => x.IsActive && x.EndDate < today)
            .ToListAsync();

        if (!expiredCampaigns.Any())
            return;

        foreach (var campaign in expiredCampaigns)
        {
            campaign.IsActive = false;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<PublicResponse> SendNewsletterAsync(SendCampaignNewsletterRequest request)
    {
        PublicResponse response = new(true);

        var campaign = await _context.MarketingCampaigns
            .FirstOrDefaultAsync(x => x.Id == request.CampaignId);

        if (campaign == null)
            return response.SetError("CampaignNotFound", "Campania nu a fost găsită.");

        var today = DateTime.UtcNow.Date;

        var isActive = campaign.IsActive
            && campaign.StartDate.Date <= today
            && campaign.EndDate.Date >= today;

        var isScheduled = campaign.IsActive
            && campaign.StartDate.Date > today;

        if (!isActive && !isScheduled)
        {
            return response.SetError(
                "InvalidCampaignStatus",
                "Newsletterul poate fi trimis doar pentru campanii active sau programate."
            );
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
            return response.SetError("SubjectRequired", "Subiectul emailului este obligatoriu.");

        if (string.IsNullOrWhiteSpace(request.HtmlContent))
            return response.SetError("ContentRequired", "Conținutul emailului este obligatoriu.");

        IQueryable<Student> studentsQuery = _context.Students
            .Where(x => !string.IsNullOrEmpty(x.Email));

        if (request.RecipientMode == "active")
        {
            studentsQuery = studentsQuery.Where(x => x.IsActive);
        }
        else if (request.RecipientMode == "inactive")
        {
            studentsQuery = studentsQuery.Where(x => !x.IsActive);
        }
        else if (request.RecipientMode == "manual")
        {
            if (request.StudentIds == null || !request.StudentIds.Any())
                return response.SetError("NoStudentsSelected", "Nu ai selectat niciun cursant.");

            studentsQuery = studentsQuery.Where(x => request.StudentIds.Contains(x.Id));
        }

        var students = await studentsQuery
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Email
            })
            .Distinct()
            .ToListAsync();

        if (!students.Any())
            return response.SetError("NoRecipients", "Nu există destinatari validați pentru newsletter.");

        var emailLog = new EmailLog
        {
            Type = EmailLogTypes.CampaignNewsletter,
            ReferenceId = campaign.Id,
            Subject = request.Subject,
            HtmlContent = request.HtmlContent,
            RecipientMode = request.RecipientMode,
            TotalRecipients = students.Count,
            SentCount = 0,
            FailedCount = 0,
            SentAt = DateTime.UtcNow
        };

        _context.EmailLogs.Add(emailLog);
        await _context.SaveChangesAsync();

        int sentCount = 0;
        int failedCount = 0;

        

        foreach (var student in students)
        {
            var recipientLog = new EmailRecipientLog
            {
                EmailLogId = emailLog.Id,
                StudentId = student.Id,
                Email = student.Email,
                Name = student.FullName,
                IsSent = false
            };

            try
            {
                var emailResponse = await _emailBusinessLogic.SendEmailAsync(
                    request.Subject,
                    request.HtmlContent,
                    new List<string> { student.Email }
                );

                if (emailResponse.IsSuccess)
                {
                    recipientLog.IsSent = true;
                    recipientLog.SentAt = DateTime.UtcNow;
                    sentCount++;
                }
                else
                {
                    recipientLog.IsSent = false;
                    recipientLog.ErrorMessage = "Emailul nu a putut fi trimis.";
                    failedCount++;
                }
            }
            catch (Exception ex)
            {
                recipientLog.IsSent = false;
                recipientLog.ErrorMessage = ex.Message;
                failedCount++;
            }

            _context.EmailRecipientLogs.Add(recipientLog);
        }

        emailLog.SentCount = sentCount;
        emailLog.FailedCount = failedCount;

        await _context.SaveChangesAsync();

        _activityLogService.Add(
            nameof(MarketingCampaign),
            campaign.Id.ToString(),
            "SendNewsletter",
            $"Newsletterul pentru campania '{campaign.Name}' a fost trimis către {sentCount} destinatari. Eșuate: {failedCount}.",
            GetCurrentUser()
        );

        await _context.SaveChangesAsync();

        await _notificationsService.CreateNotificationForRolesAsync(
             roleNames: new[] { "Admin", "Manager", "Marketing" },
             eventType: NotificationEvents.MarketingActivity,
             title: "Newsletter trimis",
             message:
                 $"Newsletterul pentru campania '{campaign.Name}' a fost trimis. " +
                 $"Succes: {sentCount}, Eșuate: {failedCount}.",
             type: failedCount > 0 ? "Warning" : "Success",
             link: $"/marketing/email-logs/{emailLog.Id}",
             entityType: nameof(EmailLog),
             entityId: emailLog.Id.ToString()
         );

        return response.SetSuccess(new
        {
            EmailLogId = emailLog.Id,
            TotalRecipients = emailLog.TotalRecipients,
            SentCount = emailLog.SentCount,
            FailedCount = emailLog.FailedCount
        });
    }

    public async Task<PublicResponse> GetCampaignNewsletterTemplateAsync(int campaignId)
    {
        PublicResponse response = new(true);

        var campaign = await _context.MarketingCampaigns
         .Include(x => x.CourseSessions)
             .ThenInclude(x => x.CourseSession)
                 .ThenInclude(cs => cs.Course) // dacă vrei și numele cursului
         .FirstOrDefaultAsync(x => x.Id == campaignId);

        if (campaign == null)
            return response.SetError("CampaignNotFound", "Campania nu a fost găsită.");

        var template = await _context.EmailTemplates
            .FirstOrDefaultAsync(x =>
                x.TemplateCode == TemplateCode.CAMPAIGN_NEWSLETTER &&
                x.IsActive);

        if (template == null)
            return response.SetError("EmailTemplateNotFound", "Template-ul de email nu a fost găsit.");

        var discount = campaign.DiscountType == DiscountType.Percentage
            ? $"{campaign.DiscountValue}%"
            : $"{campaign.DiscountValue} lei";

        string sessionsHtml = "";

        if (campaign.CourseSessions != null && campaign.CourseSessions.Any())
        {
            sessionsHtml = "<ul>";

            foreach (var rel in campaign.CourseSessions)
            {
                var session = rel.CourseSession;

                if (session == null) continue;

                sessionsHtml += $@"
            <li>
                <strong> {session.Title}
            </li>";
            }

            sessionsHtml += "</ul>";
        }
        else
        {
            sessionsHtml = "<p>Campania este disponibilă pentru toate sesiunile eligibile.</p>";
        }

        var htmlContent = template.HtmlContent
            .Replace(EmailConstants.CAMPAIGN_NAME, campaign.Name)
            .Replace(EmailConstants.CAMPAIGN_DESCRIPTION, campaign.Description ?? "")
            .Replace(EmailConstants.DISCOUNT, discount)
            .Replace(EmailConstants.START_DATE, campaign.StartDate.ToString("dd.MM.yyyy"))
            .Replace(EmailConstants.END_DATE, campaign.EndDate.ToString("dd.MM.yyyy"))
            .Replace(EmailConstants.CAMPAIGN_SESSIONS, sessionsHtml)
            .Replace(EmailConstants.YEAR, DateTime.UtcNow.Year.ToString());

        return response.SetSuccess(new
        {
            subject = template.Subject,
            htmlContent
        });
    }

    public async Task<PublicResponse> GetEmailLogsAsync(EmailLogsRequest request)
    {
        PublicResponse response = new(true);

        var query = _context.EmailLogs
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            query = query.Where(x => x.Type == request.Type);
        }

        if (request.ReferenceId.HasValue)
        {
            query = query.Where(x => x.ReferenceId == request.ReferenceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(x =>
                x.Subject.Contains(request.Search));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.Id,
                x.Type,
                x.ReferenceId,
                x.Subject,
                x.RecipientMode,
                x.TotalRecipients,
                x.SentCount,
                x.FailedCount,
                x.CreatedAt,
                x.SentAt
            })
            .ToListAsync();

        return response.SetSuccess(new
        {
            request.Page,
            request.PageSize,
            Total = total,
            Items = items
        });
    }

    public async Task<PublicResponse> GetEmailLogDetailsAsync(int emailLogId)
    {
        PublicResponse response = new(true);

        var emailLog = await _context.EmailLogs
            .Where(x => x.Id == emailLogId)
            .Select(x => new
            {
                x.Id,
                x.Type,
                x.ReferenceId,
                x.Subject,
                x.HtmlContent,
                x.RecipientMode,
                x.TotalRecipients,
                x.SentCount,
                x.FailedCount,
                x.CreatedAt,
                x.SentAt,
                Recipients = x.Recipients.Select(r => new
                {
                    r.Id,
                    r.StudentId,
                    r.Name,
                    r.Email,
                    r.IsSent,
                    r.ErrorMessage,
                    r.SentAt
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (emailLog == null)
            return response.SetError("EmailLogNotFound", "Emailul nu a fost găsit.");

        return response.SetSuccess(emailLog);
    }

    private string GetCurrentUser()
    {
        return _httpContextAccessor.HttpContext?.User?
            .FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? _httpContextAccessor.HttpContext?.User?
                .FindFirst("email")?.Value
            ?? _httpContextAccessor.HttpContext?.User?
                .FindFirst("username")?.Value
            ?? "system";
    }


}