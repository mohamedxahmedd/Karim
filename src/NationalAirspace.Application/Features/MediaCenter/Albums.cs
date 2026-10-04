using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.MediaCenter;

public class AlbumInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleEn { get; set; } = string.Empty;
    [StringLength(4000, ErrorMessage = V.MaxLength)] public string? DescriptionAr { get; set; }
    [StringLength(4000, ErrorMessage = V.MaxLength)] public string? DescriptionEn { get; set; }
    [DataType(DataType.Date)] public DateTime? EventDate { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
}

public class AlbumPhotoCaptionInput
{
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? CaptionAr { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? CaptionEn { get; set; }
}

public sealed record AlbumAdminListItem(Guid Id, string TitleAr, string TitleEn, ContentStatus Status, int PhotoCount, string? CoverUrl, DateTime? EventDate, int DisplayOrder);

public sealed record AlbumPhotoAdminItem(Guid Id, Guid MediaFileId, string Url, string? CaptionAr, string? CaptionEn, int DisplayOrder, bool IsCover);

public sealed record AlbumCardDto(Guid Id, string Title, string? Description, ImageRef? Cover, int PhotoCount, DateTime? EventDate);

public sealed record AlbumPhotoDto(string Url, string? Caption, string Alt);

public sealed record AlbumDetailDto(Guid Id, string Title, string? Description, DateTime? EventDate, IReadOnlyList<AlbumPhotoDto> Photos);

public sealed class AlbumAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "Album";

    public async Task<PagedResult<AlbumAdminListItem>> ListAsync(AdminListQuery q, CancellationToken ct = default)
    {
        var query = Db.PhotoAlbums.AsNoTracking();
        if (q.Term is { } term) query = query.Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term));
        if (q.Status is { } status) query = query.Where(x => x.Status == status);
        var paged = await query.OrderBy(x => x.DisplayOrder)
            .Select(x => new { x.Id, x.TitleAr, x.TitleEn, x.Status, Count = x.Photos.Count, Cover = x.CoverMedia != null ? x.CoverMedia.StoragePath : null, x.EventDate, x.DisplayOrder })
            .ToPagedAsync(q.Page, q.PageSize, ct);
        return paged.Map(x => new AlbumAdminListItem(x.Id, x.TitleAr, x.TitleEn, x.Status, x.Count, MediaUrls.Image(x.Cover), x.EventDate, x.DisplayOrder));
    }

    public async Task<AlbumInput?> GetForEditAsync(Guid id, CancellationToken ct = default) =>
        await Db.PhotoAlbums.AsNoTracking().Where(x => x.Id == id).Select(x => new AlbumInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, TitleAr = x.TitleAr, TitleEn = x.TitleEn,
            DescriptionAr = x.DescriptionAr, DescriptionEn = x.DescriptionEn, EventDate = x.EventDate, Status = x.Status,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveAsync(AlbumInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        if (errors.Count > 0) return Result<Guid>.Failure(errors);
        var entity = input.Id is { } id ? await Db.PhotoAlbums.FirstOrDefaultAsync(x => x.Id == id, ct) : new PhotoAlbum();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);

        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.DescriptionAr = Clean(input.DescriptionAr);
        entity.DescriptionEn = Clean(input.DescriptionEn);
        entity.EventDate = input.EventDate?.AsUtcDate();
        entity.ChangeStatus(input.Status, Clock.UtcNow);
        if (input.Id is null)
        {
            entity.DisplayOrder = await NextOrderAsync(Db.PhotoAlbums, ct);
            Db.PhotoAlbums.Add(entity);
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
        ChangeStatusAsync(Db.PhotoAlbums, id, status, x => x.TitleAr, ct);

    public Task<Result> MoveAsync(Guid id, int direction, CancellationToken ct = default) => MoveAsync(Db.PhotoAlbums, id, direction, ct);

    /// <summary>Deletes the album and its photo links. Image files remain in the media library.</summary>
    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) => DeleteAsync(Db.PhotoAlbums, id, x => x.TitleAr, ct);

    // ---- photos ----

    public async Task<IReadOnlyList<AlbumPhotoAdminItem>> GetPhotosAsync(Guid albumId, CancellationToken ct = default)
    {
        var album = await Db.PhotoAlbums.AsNoTracking().FirstOrDefaultAsync(a => a.Id == albumId, ct);
        if (album is null) return [];
        var photos = await Db.AlbumPhotos.AsNoTracking().Where(p => p.AlbumId == albumId).OrderBy(p => p.DisplayOrder)
            .Select(p => new { p.Id, p.MediaFileId, p.MediaFile!.StoragePath, p.CaptionAr, p.CaptionEn, p.DisplayOrder }).ToListAsync(ct);
        return photos.Select(p => new AlbumPhotoAdminItem(p.Id, p.MediaFileId, MediaUrls.Image(p.StoragePath)!, p.CaptionAr, p.CaptionEn,
            p.DisplayOrder, p.MediaFileId == album.CoverMediaId)).ToList();
    }

    /// <summary>Adds uploaded library images to the album. The first photo becomes the cover if none is set.</summary>
    public async Task<Result> AddPhotosAsync(Guid albumId, IReadOnlyCollection<Guid> mediaIds, CancellationToken ct = default)
    {
        var album = await Db.PhotoAlbums.FirstOrDefaultAsync(a => a.Id == albumId, ct);
        if (album is null) return Result.Failure(Error.NotFound);
        var valid = await Db.MediaFiles.Where(m => mediaIds.Contains(m.Id) && m.Kind == MediaKind.Image).Select(m => m.Id).ToListAsync(ct);
        if (valid.Count == 0) return Result.Failure(new Error("Albums.NoImages"));
        var order = await NextOrderAsync(Db.AlbumPhotos.Where(p => p.AlbumId == albumId), ct);
        foreach (var mediaId in mediaIds.Where(valid.Contains))
            Db.AlbumPhotos.Add(new AlbumPhoto { AlbumId = albumId, MediaFileId = mediaId, DisplayOrder = order++ });
        album.CoverMediaId ??= valid[0];
        Audit("AddPhotos", albumId, $"{valid.Count} photo(s)");
        return await CommitAsync(ct);
    }

    public async Task<Result> UpdatePhotoCaptionAsync(Guid photoId, AlbumPhotoCaptionInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        if (errors.Count > 0) return Result.Failure(errors);
        var photo = await Db.AlbumPhotos.FirstOrDefaultAsync(p => p.Id == photoId, ct);
        if (photo is null) return Result.Failure(Error.NotFound);
        photo.CaptionAr = Clean(input.CaptionAr);
        photo.CaptionEn = Clean(input.CaptionEn);
        Audit("UpdatePhoto", photo.AlbumId, null);
        return await CommitAsync(ct);
    }

    public async Task<Result> SetCoverAsync(Guid photoId, CancellationToken ct = default)
    {
        var photo = await Db.AlbumPhotos.Include(p => p.Album).FirstOrDefaultAsync(p => p.Id == photoId, ct);
        if (photo?.Album is null) return Result.Failure(Error.NotFound);
        photo.Album.CoverMediaId = photo.MediaFileId;
        Audit("SetCover", photo.AlbumId, null);
        return await CommitAsync(ct);
    }

    public async Task<Result> MovePhotoAsync(Guid photoId, int direction, CancellationToken ct = default)
    {
        var photo = await Db.AlbumPhotos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == photoId, ct);
        if (photo is null) return Result.Failure(Error.NotFound);
        return await MoveAsync(Db.AlbumPhotos.Where(p => p.AlbumId == photo.AlbumId), photoId, direction, ct);
    }

    public async Task<Result> RemovePhotoAsync(Guid photoId, CancellationToken ct = default)
    {
        var photo = await Db.AlbumPhotos.Include(p => p.Album).FirstOrDefaultAsync(p => p.Id == photoId, ct);
        if (photo?.Album is null) return Result.Failure(Error.NotFound);
        if (photo.Album.CoverMediaId == photo.MediaFileId)
        {
            photo.Album.CoverMediaId = await Db.AlbumPhotos.Where(p => p.AlbumId == photo.AlbumId && p.Id != photoId)
                .OrderBy(p => p.DisplayOrder).Select(p => (Guid?)p.MediaFileId).FirstOrDefaultAsync(ct);
        }
        Db.AlbumPhotos.Remove(photo);
        Audit("RemovePhoto", photo.AlbumId, null);
        return await CommitAsync(ct);
    }

    public async Task<Guid?> GetAlbumIdOfPhotoAsync(Guid photoId, CancellationToken ct = default) =>
        await Db.AlbumPhotos.AsNoTracking().Where(p => p.Id == photoId).Select(p => (Guid?)p.AlbumId).FirstOrDefaultAsync(ct);
}

public sealed class AlbumPublicService(IApplicationDbContext db)
{
    public async Task<PagedResult<AlbumCardDto>> ListAsync(Lang lang, int page, int pageSize = 12, CancellationToken ct = default)
    {
        var paged = await db.PhotoAlbums.AsNoTracking().Published()
            .Where(a => a.Photos.Any()).OrderBy(a => a.DisplayOrder)
            .Select(a => new
            {
                a.Id, a.TitleAr, a.TitleEn, a.DescriptionAr, a.DescriptionEn, a.EventDate, Count = a.Photos.Count,
                CoverPath = a.CoverMedia != null ? a.CoverMedia.StoragePath : null,
                CoverAltAr = a.CoverMedia != null ? a.CoverMedia.AltTextAr : null,
                CoverAltEn = a.CoverMedia != null ? a.CoverMedia.AltTextEn : null,
            })
            .ToPagedAsync(page, pageSize, ct);
        return paged.Map(x =>
        {
            var title = lang.Pick(x.TitleAr, x.TitleEn);
            var cover = x.CoverPath is null ? null : new ImageRef(MediaUrls.Image(x.CoverPath)!, lang.PickOrNull(x.CoverAltAr, x.CoverAltEn) ?? title);
            return new AlbumCardDto(x.Id, title, lang.PickOrNull(x.DescriptionAr, x.DescriptionEn), cover, x.Count, x.EventDate);
        });
    }

    public async Task<AlbumDetailDto?> GetAsync(Lang lang, Guid id, CancellationToken ct = default)
    {
        var album = await db.PhotoAlbums.AsNoTracking().Published()
            .Include(a => a.Photos.OrderBy(p => p.DisplayOrder)).ThenInclude(p => p.MediaFile)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (album is null) return null;
        var title = lang.Pick(album.TitleAr, album.TitleEn);
        return new AlbumDetailDto(album.Id, title, lang.PickOrNull(album.DescriptionAr, album.DescriptionEn), album.EventDate,
            album.Photos.OrderBy(p => p.DisplayOrder).Where(p => p.MediaFile is not null).Select(p =>
            {
                var caption = lang.PickOrNull(p.CaptionAr, p.CaptionEn);
                return new AlbumPhotoDto(MediaUrls.Image(p.MediaFile!.StoragePath)!, caption,
                    caption ?? lang.PickOrNull(p.MediaFile.AltTextAr, p.MediaFile.AltTextEn) ?? title);
            }).ToList());
    }
}
