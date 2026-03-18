using ERPSystem.Data.Context;
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

    public async Task<List<ActivityLogDto>> GetActivity(string entityType, int entityId)
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


    public async Task<PagedResult<ActivityLogDto>> GetAllActivity( string? entityType, string? action, string? performedBy, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var query = _db.ActivityLog.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(x => x.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(x => x.Action == action);

        if (!string.IsNullOrWhiteSpace(performedBy))
            query = query.Where(x => x.PerformedBy == performedBy);

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

        return new PagedResult<ActivityLogDto>(
            page,
            pageSize,
            total,
            logs.Select(log => new ActivityLogDto
            {
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Action = log.Action,
                CreatedAtUtc = log.CreatedAtUtc,
                Description = log.Description,
                PerformedBy = log.PerformedBy
            }).ToList()
        );
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
}