using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;

namespace NationalAirspace.Application.Features.Audit;

public sealed record AuditLogItem(DateTime CreatedAtUtc, string UserName, string Action, string EntityType, string? EntityId, string? Summary, string? IpAddress);

public sealed class AuditQueryService(IApplicationDbContext db)
{
    public async Task<PagedResult<AuditLogItem>> ListAsync(string? search, string? entityType, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => a.UserName.Contains(term) || a.Action.Contains(term) || (a.Summary != null && a.Summary.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(a => a.EntityType == entityType);
        return await query.OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new AuditLogItem(a.CreatedAtUtc, a.UserName, a.Action, a.EntityType, a.EntityId, a.Summary, a.IpAddress))
            .ToPagedAsync(page, pageSize, ct);
    }

    public async Task<IReadOnlyList<string>> GetEntityTypesAsync(CancellationToken ct = default) =>
        await db.AuditLogs.AsNoTracking().Select(a => a.EntityType).Distinct().OrderBy(t => t).ToListAsync(ct);
}
