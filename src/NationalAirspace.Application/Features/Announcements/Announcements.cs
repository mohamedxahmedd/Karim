using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Announcements;

public class AnnouncementInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleEn { get; set; } = string.Empty;
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
    public Guid? AttachmentId { get; set; }
    public DateTime? PublishDateLocal { get; set; }
    public DateTime? ExpiresAtLocal { get; set; }
    public bool IsImportant { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
}

public sealed record AnnouncementAdminListItem(Guid Id, string TitleAr, string TitleEn, ContentStatus Status, bool IsImportant,
    DateTime? PublishDateUtc, DateTime? ExpiresAtUtc, bool IsExpired, bool IsScheduled, bool HasAttachment);

public sealed record AnnouncementDto(Guid Id, string Title, string? ContentHtml, DateTime PublishDateUtc, DateTime? ExpiresAtUtc,
    bool IsImportant, bool IsExpired, string? AttachmentName, long? AttachmentSize);

public sealed class AnnouncementAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "Announcement";

    public async Task<PagedResult<AnnouncementAdminListItem>> ListAsync(AdminListQuery q, CancellationToken ct = default)
    {
        var query = Db.Announcements.AsNoTracking();
        if (q.Term is { } term) query = query.Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term));
        if (q.Status is { } status) query = query.Where(x => x.Status == status);
        var now = Clock.UtcNow;
        return await query.OrderByDescending(x => x.PublishDateUtc ?? x.CreatedAtUtc)
            .Select(x => new AnnouncementAdminListItem(x.Id, x.TitleAr, x.TitleEn, x.Status, x.IsImportant, x.PublishDateUtc, x.ExpiresAtUtc,
                x.ExpiresAtUtc != null && x.ExpiresAtUtc <= now, x.Status == ContentStatus.Published && x.PublishDateUtc > now, x.AttachmentId != null))
            .ToPagedAsync(q.Page, q.PageSize, ct);
    }

    public async Task<AnnouncementInput?> GetForEditAsync(Guid id, CancellationToken ct = default)
    {
        var x = await Db.Announcements.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        return x is null ? null : new AnnouncementInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, TitleAr = x.TitleAr, TitleEn = x.TitleEn,
            ContentAr = x.ContentAr, ContentEn = x.ContentEn, AttachmentId = x.AttachmentId,
            PublishDateLocal = Clock.ToSiteTime(x.PublishDateUtc), ExpiresAtLocal = Clock.ToSiteTime(x.ExpiresAtUtc),
            IsImportant = x.IsImportant, Status = x.Status,
        };
    }

    public async Task<Result<Guid>> SaveAsync(AnnouncementInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        var entity = input.Id is { } id ? await Db.Announcements.FirstOrDefaultAsync(x => x.Id == id, ct) : new Announcement();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        if (!await MediaService.ExistsAsync(Db, input.AttachmentId, MediaKind.Document, ct))
            errors.Add(new Error("Val.InvalidOption", nameof(input.AttachmentId)));
        var publish = Clock.FromSiteTime(input.PublishDateLocal);
        var expires = Clock.FromSiteTime(input.ExpiresAtLocal);
        if (expires is { } e && e <= (publish ?? Clock.UtcNow) && input.Status == ContentStatus.Published)
            errors.Add(new Error("Announcements.ExpiryBeforePublish", nameof(input.ExpiresAtLocal)));
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.ContentAr = SanitizeHtml(input.ContentAr);
        entity.ContentEn = SanitizeHtml(input.ContentEn);
        entity.AttachmentId = input.AttachmentId;
        entity.PublishDateUtc = publish;
        entity.ExpiresAtUtc = expires;
        entity.IsImportant = input.IsImportant;
        entity.ChangeStatus(input.Status, Clock.UtcNow);

        if (input.Id is null)
        {
            Db.Announcements.Add(entity);
            Audit("Create", entity.Id, entity.TitleAr);
        }
        else
        {
            ExpectStamp(entity, input.ConcurrencyStamp);
            Audit("Update", entity.Id, entity.TitleAr);
        }
        return await CommitAsync(entity.Id, ct);
    }

    public Task<Result> ChangeStatusAsync(Guid id, ContentStatus status, CancellationToken ct = default) =>
        ChangeStatusAsync(Db.Announcements, id, status, x => x.TitleAr, ct);

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync(Db.Announcements, id, x => x.TitleAr, ct);

    /// <summary>Archives every published announcement whose expiry has passed. Returns the number archived.</summary>
    public async Task<int> ArchiveExpiredAsync(CancellationToken ct = default)
    {
        var now = Clock.UtcNow;
        var expired = await Db.Announcements
            .Where(a => a.Status == ContentStatus.Published && a.ExpiresAtUtc != null && a.ExpiresAtUtc <= now)
            .ToListAsync(ct);
        foreach (var a in expired)
        {
            if (a.ArchiveIfExpired(now)) Audit("AutoArchive", a.Id, a.TitleAr);
        }
        if (expired.Count > 0) await CommitAsync(ct);
        return expired.Count;
    }
}

public sealed class AnnouncementPublicService(IApplicationDbContext db, IClock clock)
{
    /// <summary>Active announcements: important first, then newest.</summary>
    public async Task<PagedResult<AnnouncementDto>> GetActiveAsync(Lang lang, int page, int pageSize = 10, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var paged = await db.Announcements.AsNoTracking().ActiveAt(now).Include(a => a.Attachment)
            .OrderByDescending(a => a.IsImportant).ThenByDescending(a => a.PublishDateUtc)
            .ToPagedAsync(page, pageSize, ct);
        return paged.Map(a => ToDto(a, lang, now));
    }

    /// <summary>Past announcements: expired or archived after having been published.</summary>
    public async Task<PagedResult<AnnouncementDto>> GetArchiveAsync(Lang lang, int page, int pageSize = 10, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var paged = await db.Announcements.AsNoTracking().Include(a => a.Attachment)
            .Where(a => a.PublishDateUtc != null && a.PublishDateUtc <= now
                        && ((a.Status == ContentStatus.Published && a.ExpiresAtUtc != null && a.ExpiresAtUtc <= now)
                            || a.Status == ContentStatus.Archived))
            .OrderByDescending(a => a.PublishDateUtc)
            .ToPagedAsync(page, pageSize, ct);
        return paged.Map(a => ToDto(a, lang, now));
    }

    /// <summary>Details are available for active and archived (previously published) announcements.</summary>
    public async Task<AnnouncementDto?> GetAsync(Lang lang, Guid id, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var a = await db.Announcements.AsNoTracking().Include(x => x.Attachment)
            .FirstOrDefaultAsync(x => x.Id == id && x.Status != ContentStatus.Draft && x.PublishDateUtc != null && x.PublishDateUtc <= now, ct);
        return a is null ? null : ToDto(a, lang, now);
    }

    /// <summary>Attachment of a publicly visible (active or archived) announcement.</summary>
    public async Task<Guid?> GetAttachmentIdAsync(Guid id, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        return await db.Announcements.AsNoTracking()
            .Where(x => x.Id == id && x.Status != ContentStatus.Draft && x.PublishDateUtc != null && x.PublishDateUtc <= now)
            .Select(x => x.AttachmentId).FirstOrDefaultAsync(ct);
    }

    private static AnnouncementDto ToDto(Announcement a, Lang lang, DateTime now) => new(
        a.Id, lang.Pick(a.TitleAr, a.TitleEn), lang.PickOrNull(a.ContentAr, a.ContentEn), a.PublishDateUtc ?? a.CreatedAtUtc,
        a.ExpiresAtUtc, a.IsImportant, a.IsExpired(now) || a.Status == ContentStatus.Archived,
        a.Attachment?.OriginalFileName, a.Attachment?.SizeBytes);
}
