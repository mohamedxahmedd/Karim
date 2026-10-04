using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;
using NationalAirspace.Domain.ValueObjects;

namespace NationalAirspace.Application.Features.MediaCenter;

public class VideoInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleEn { get; set; } = string.Empty;
    [StringLength(4000, ErrorMessage = V.MaxLength)] public string? DescriptionAr { get; set; }
    [StringLength(4000, ErrorMessage = V.MaxLength)] public string? DescriptionEn { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(500, ErrorMessage = V.MaxLength)] public string Url { get; set; } = string.Empty;
    public Guid? ThumbnailId { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
}

public sealed record VideoAdminListItem(Guid Id, string TitleAr, string TitleEn, VideoProvider Provider, string Url, string? ThumbnailUrl, ContentStatus Status, int DisplayOrder);

public sealed record VideoDto(Guid Id, string Title, string? Description, string Url, string? EmbedUrl, string? ThumbnailUrl, VideoProvider Provider);

public sealed class VideoAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "Video";

    public async Task<PagedResult<VideoAdminListItem>> ListAsync(AdminListQuery q, CancellationToken ct = default)
    {
        var query = Db.Videos.AsNoTracking();
        if (q.Term is { } term) query = query.Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term));
        if (q.Status is { } status) query = query.Where(x => x.Status == status);
        var paged = await query.OrderBy(x => x.DisplayOrder)
            .Select(x => new { x.Id, x.TitleAr, x.TitleEn, x.Provider, x.Url, x.ExternalId, Thumb = x.Thumbnail != null ? x.Thumbnail.StoragePath : null, x.Status, x.DisplayOrder })
            .ToPagedAsync(q.Page, q.PageSize, ct);
        return paged.Map(x => new VideoAdminListItem(x.Id, x.TitleAr, x.TitleEn, x.Provider, x.Url,
            MediaUrls.Image(x.Thumb) ?? ThumbFor(x.Provider, x.Url), x.Status, x.DisplayOrder));
    }

    public async Task<VideoInput?> GetForEditAsync(Guid id, CancellationToken ct = default) =>
        await Db.Videos.AsNoTracking().Where(x => x.Id == id).Select(x => new VideoInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, TitleAr = x.TitleAr, TitleEn = x.TitleEn,
            DescriptionAr = x.DescriptionAr, DescriptionEn = x.DescriptionEn, Url = x.Url, ThumbnailId = x.ThumbnailId, Status = x.Status,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveAsync(VideoInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        if (!VideoLink.TryCreate(input.Url, out var link)) errors.Add(new Error("Videos.InvalidUrl", nameof(input.Url)));
        if (!await MediaService.ExistsAsync(Db, input.ThumbnailId, MediaKind.Image, ct))
            errors.Add(new Error("Val.InvalidOption", nameof(input.ThumbnailId)));
        var entity = input.Id is { } id ? await Db.Videos.FirstOrDefaultAsync(x => x.Id == id, ct) : new Video();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.DescriptionAr = Clean(input.DescriptionAr);
        entity.DescriptionEn = Clean(input.DescriptionEn);
        entity.Url = link!.Url;
        entity.Provider = link.Provider;
        entity.ExternalId = link.ExternalId;
        entity.ThumbnailId = input.ThumbnailId;
        entity.ChangeStatus(input.Status, Clock.UtcNow);
        if (input.Id is null)
        {
            entity.DisplayOrder = await NextOrderAsync(Db.Videos, ct);
            Db.Videos.Add(entity);
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
        ChangeStatusAsync(Db.Videos, id, status, x => x.TitleAr, ct);

    public Task<Result> MoveAsync(Guid id, int direction, CancellationToken ct = default) => MoveAsync(Db.Videos, id, direction, ct);

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) => DeleteAsync(Db.Videos, id, x => x.TitleAr, ct);

    internal static string? ThumbFor(VideoProvider provider, string url) =>
        provider == VideoProvider.YouTube && VideoLink.TryCreate(url, out var l) ? l!.ThumbnailUrl : null;
}

public sealed class VideoPublicService(IApplicationDbContext db)
{
    public async Task<PagedResult<VideoDto>> ListAsync(Lang lang, int page, int pageSize = 9, CancellationToken ct = default)
    {
        var paged = await db.Videos.AsNoTracking().Published().Include(v => v.Thumbnail).OrderBy(v => v.DisplayOrder).ToPagedAsync(page, pageSize, ct);
        return paged.Map(v =>
        {
            VideoLink.TryCreate(v.Url, out var link);
            return new VideoDto(v.Id, lang.Pick(v.TitleAr, v.TitleEn), lang.PickOrNull(v.DescriptionAr, v.DescriptionEn), v.Url,
                link?.EmbedUrl, MediaUrls.Image(v.Thumbnail?.StoragePath) ?? link?.ThumbnailUrl, v.Provider);
        });
    }
}
