using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Student.Models;
using ERPSystem.Shared.ActivityLogs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class ActivityLogService
{
    private readonly ApplicationDbContext _db;

    public ActivityLogService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<ActivityLogDto>> GetActivity(string entityType, string entityId)
    {
        var logs = await _db.ActivityLog
            .AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        return logs.Select(log =>
        {
            Dictionary<string, Dictionary<string, object?>> changes;

            try
            {
                changes = string.IsNullOrEmpty(log.Changes)
                    ? new()
                    : JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object?>>>(log.Changes)
                      ?? new();
            }
            catch
            {
                changes = new();
            }

            return new ActivityLogDto
            {
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Action = log.Action,
                CreatedAtUtc = log.CreatedAtUtc,
                Description = log.Description,
                OldValues = changes.ContainsKey("Old") ? changes["Old"] : new(),
                NewValues = changes.ContainsKey("New") ? changes["New"] : new()
            };
        }).ToList();
    }

    public async Task<PagedResult<ActivityLogDto>> GetAllActivity(List<string>? entityTypes, List<string>? actions, List<string>? performedBy, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = _db.ActivityLog.AsQueryable();

        if (entityTypes != null && entityTypes.Any())
            query = query.Where(x => entityTypes.Contains(x.EntityType));

        if (actions != null && actions.Any())
            query = query.Where(x => actions.Contains(x.Action));

        if (performedBy != null && performedBy.Any())
            query = query.Where(x => performedBy.Contains(x.PerformedBy));

        if (from.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.CreatedAtUtc <= to.Value);

        var total = await query.CountAsync();

        var logs = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
       
        return new PagedResult<ActivityLogDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = logs.Select(log => new ActivityLogDto
            {
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Action = log.Action,
                CreatedAtUtc = log.CreatedAtUtc,
                Description = log.Description,
                PerformedBy = log.PerformedBy
            }).ToList()
        };
    }

    public async Task<object> GetFilterOptions()
    {
        var entities = await _db.ActivityLog
            .Select(x => x.EntityType)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var actions = await _db.ActivityLog
            .Select(x => x.Action)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var users = await _db.ActivityLog
           .Select(x => x.PerformedBy)
           .Distinct()
           .OrderBy(x => x)
           .ToListAsync();

        return new
        {
            entities,
            actions, 
            users
        };
    }

    public void Add(  string entityType, string entityId,  string action, string description, string? performedBy = null)
    {
        _db.ActivityLog.Add(new ActivityLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Description = description,
            CreatedAtUtc = DateTime.UtcNow,
            PerformedBy = string.IsNullOrWhiteSpace(performedBy)
                ? "system"
                : performedBy
        });
    }
}