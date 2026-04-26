using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.MarketingCampaign.Models;
using ERPSystem.Utils.Enums;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;

public class MarketingCampaignService 
{
    private readonly ApplicationDbContext _context;

    public MarketingCampaignService(ApplicationDbContext context)
    {
        _context = context;
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

        return new PublicResponse(true)
            .SetSuccess("Campania a fost ștearsă cu succes.");
    }

    public async Task<PublicResponse> ToggleActiveAsync(int id, DateTime? endDate)
    {
        var campaign = await _context.MarketingCampaigns.FindAsync(id);

        if (campaign == null)
            return new PublicResponse(false)
                .BadRequest("Campania nu a fost găsită.", "CampaignNotFound");

        // 🔥 dacă o activezi
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
            // 🔴 dezactivare simplă
            campaign.IsActive = false;
        }

        await _context.SaveChangesAsync();

        return new PublicResponse(true).SetSuccess();
    }

    private static PublicResponse ValidateCampaign(MarketingCampaignDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new PublicResponse(false)
                .BadRequest("Numele campaniei este obligatoriu.", "CampaignNameRequired");

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


}