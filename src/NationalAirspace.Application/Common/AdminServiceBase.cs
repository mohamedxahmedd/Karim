using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Domain.Common;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Common;

/// <summary>Shared plumbing for admin (write-side) services: auditing, saving, reordering, status changes.</summary>
public abstract class AdminServiceBase(AdminServiceDependencies deps)
{
    protected IApplicationDbContext Db { get; } = deps.Db;
    protected IClock Clock { get; } = deps.Clock;
    protected ICurrentUser CurrentUser { get; } = deps.CurrentUser;
    protected IHtmlSanitizer Sanitizer { get; } = deps.Sanitizer;
    private readonly IContentCacheInvalidator _cache = deps.Cache;

    /// <summary>Entity type name recorded in the audit log.</summary>
    protected abstract string AuditEntityType { get; }

    protected void Audit(string action, Guid? entityId, string? summary)
    {
        Db.AuditLogs.Add(new AuditLog
        {
            CreatedAtUtc = Clock.UtcNow,
            UserId = CurrentUser.UserId,
            UserName = CurrentUser.UserName,
            Action = action,
            EntityType = AuditEntityType,
            EntityId = entityId?.ToString(),
            Summary = summary?.Length > 400 ? summary[..400] : summary,
            IpAddress = CurrentUser.IpAddress,
        });
    }

    /// <summary>Marks <paramref name="entity"/> as expected to still have <paramref name="stamp"/> in the database.</summary>
    protected void ExpectStamp(AuditableEntity entity, Guid stamp) =>
        Db.Entry(entity).Property(e => e.ConcurrencyStamp).OriginalValue = stamp;

    /// <summary>Saves changes, maps concurrency conflicts to a failure and evicts public caches.</summary>
    protected async Task<Result> CommitAsync(CancellationToken ct)
    {
        try
        {
            await Db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(Error.Concurrency);
        }
        await _cache.InvalidateAsync(ct);
        return Result.Success();
    }

    protected async Task<Result<Guid>> CommitAsync(Guid id, CancellationToken ct)
    {
        var result = await CommitAsync(ct);
        return result.Succeeded ? Result.Success(id) : Result<Guid>.Failure(result.Errors);
    }

    protected string? SanitizeHtml(string? html) => Sanitizer.Sanitize(html);

    protected static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    protected async Task<Result> ChangeStatusAsync<T>(DbSet<T> set, Guid id, ContentStatus status, Func<T, string> describe, CancellationToken ct)
        where T : PublishableEntity
    {
        var entity = await set.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null) return Result.Failure(Error.NotFound);
        entity.ChangeStatus(status, Clock.UtcNow);
        Audit($"Status:{status}", id, describe(entity));
        return await CommitAsync(ct);
    }

    protected async Task<Result> DeleteAsync<T>(DbSet<T> set, Guid id, Func<T, string> describe, CancellationToken ct)
        where T : Entity
    {
        var entity = await set.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null) return Result.Failure(Error.NotFound);
        set.Remove(entity);
        Audit("Delete", id, describe(entity));
        return await CommitAsync(ct);
    }

    /// <summary>
    /// Moves an item one position up (<paramref name="direction"/> &lt; 0) or down among its siblings,
    /// normalizing the sibling sequence to 1..n.
    /// </summary>
    protected async Task<Result> MoveAsync<T>(IQueryable<T> siblings, Guid id, int direction, CancellationToken ct)
        where T : class, IOrderable
    {
        var items = await siblings.OrderBy(x => x.DisplayOrder).ToListAsync(ct);
        var index = items.FindIndex(x => x.Id == id);
        if (index < 0) return Result.Failure(Error.NotFound);
        var target = index + Math.Sign(direction);
        if (target >= 0 && target < items.Count)
            (items[index], items[target]) = (items[target], items[index]);
        for (var i = 0; i < items.Count; i++) items[i].DisplayOrder = i + 1;
        Audit("Reorder", id, null);
        return await CommitAsync(ct);
    }

    /// <summary>
    /// Resolves both slugs: explicit slugs must be unique (error otherwise); empty slugs are generated
    /// from the titles and made unique automatically. Slugs are unique across both language columns.
    /// </summary>
    protected static async Task<SlugPair> ResolveSlugsAsync<T>(IQueryable<T> set, Guid id, string? requestedAr, string? requestedEn,
        string titleAr, string titleEn, List<Error> errors, CancellationToken ct)
        where T : class, ISluggable
    {
        async Task<bool> Taken(string slug) => await set.AnyAsync(x => x.Id != id && (x.SlugAr == slug || x.SlugEn == slug), ct);

        async Task<string> Resolve(string? requested, string title, string fallback, string field)
        {
            if (!string.IsNullOrWhiteSpace(requested))
            {
                var slug = requested.Trim().ToLowerInvariant();
                if (await Taken(slug)) errors.Add(new Error(V.SlugTaken, field));
                return slug;
            }
            var generated = SlugGenerator.Generate(title);
            if (generated.Length == 0) generated = SlugGenerator.Generate(fallback);
            if (generated.Length == 0) generated = id.ToString("N")[..12];
            return await SlugGenerator.EnsureUniqueAsync(generated, Taken);
        }

        var ar = await Resolve(requestedAr, titleAr, titleEn, "SlugAr");
        var en = await Resolve(requestedEn, titleEn, titleAr, "SlugEn");
        return new SlugPair(ar, en);
    }

    protected static async Task<int> NextOrderAsync<T>(IQueryable<T> siblings, CancellationToken ct) where T : class, IOrderable =>
        (await siblings.MaxAsync(x => (int?)x.DisplayOrder, ct) ?? 0) + 1;
}

/// <summary>Bundles the dependencies every admin service needs (keeps constructors short).</summary>
public sealed record AdminServiceDependencies(
    IApplicationDbContext Db,
    IClock Clock,
    ICurrentUser CurrentUser,
    IHtmlSanitizer Sanitizer,
    IContentCacheInvalidator Cache);

/// <summary>Filter for admin list screens.</summary>
public sealed record AdminListQuery(string? Search = null, ContentStatus? Status = null, Guid? CategoryId = null, int Page = 1, int PageSize = 20)
{
    public string? Term => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
}
