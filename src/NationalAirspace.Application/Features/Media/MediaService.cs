using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Media;

/// <summary>Media library: validated uploads, metadata, safe deletion and downloads.</summary>
public sealed class MediaService(AdminServiceDependencies deps, IFileStorage storage, IOptions<MediaOptions> options)
    : AdminServiceBase(deps)
{
    private readonly MediaOptions _options = options.Value;

    protected override string AuditEntityType => "Media";

    public async Task<Result<MediaFileDto>> UploadAsync(Stream content, string originalFileName, long length, MediaKind expectedKind, CancellationToken ct = default)
    {
        var fileName = Path.GetFileName(originalFileName ?? string.Empty).Trim();
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (fileName.Length == 0 || length <= 0)
            return Result<MediaFileDto>.Failure(new Error("Media.Empty"));
        if (!FileSignatures.TryGetType(extension, out var kind, out var contentType) || kind != expectedKind)
            return Result<MediaFileDto>.Failure(new Error("Media.TypeNotAllowed", null, FileSignatures.AcceptAttribute(expectedKind)));
        var max = _options.MaxBytesFor(kind);
        if (length > max)
            return Result<MediaFileDto>.Failure(new Error("Media.TooLarge", null, max / (1024 * 1024)));

        // Work on a seekable stream so the header can be inspected and the content stored.
        Stream source = content;
        MemoryStream? buffer = null;
        if (!content.CanSeek)
        {
            buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            source = buffer;
        }

        try
        {
            source.Position = 0;
            var header = new byte[FileSignatures.HeaderLength];
            var read = await source.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, ct);
            if (!FileSignatures.Matches(extension, header.AsSpan(0, read)))
                return Result<MediaFileDto>.Failure(new Error("Media.SignatureMismatch"));
            source.Position = 0;

            var path = await storage.SaveAsync(source, kind, extension, ct);
            var entity = new MediaFile
            {
                OriginalFileName = fileName.Length > 255 ? fileName[^255..] : fileName,
                StoragePath = path,
                ContentType = contentType,
                SizeBytes = length,
                Kind = kind,
                UploadedByUserId = CurrentUser.UserId,
            };
            Db.MediaFiles.Add(entity);
            Audit("Upload", entity.Id, entity.OriginalFileName);
            var result = await CommitAsync(ct);
            if (!result.Succeeded)
            {
                await storage.DeleteAsync(path, kind, ct);
                return Result<MediaFileDto>.Failure(result.Errors);
            }
            return ToDto(entity);
        }
        finally
        {
            if (buffer is not null) await buffer.DisposeAsync();
        }
    }

    public async Task<PagedResult<MediaFileDto>> ListAsync(string? search, MediaKind? kind, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Db.MediaFiles.AsNoTracking();
        if (kind is { } k) query = query.Where(m => m.Kind == k);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m => m.OriginalFileName.Contains(term)
                                  || (m.AltTextAr != null && m.AltTextAr.Contains(term))
                                  || (m.AltTextEn != null && m.AltTextEn.Contains(term)));
        }
        var paged = await query.OrderByDescending(m => m.CreatedAtUtc).ToPagedAsync(page, pageSize, ct);
        return paged.Map(ToDto);
    }

    public async Task<MediaFileDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await Db.MediaFiles.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<Result> UpdateMetadataAsync(Guid id, MediaMetadataInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        if (errors.Count > 0) return Result.Failure(errors);
        var entity = await Db.MediaFiles.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity is null) return Result.Failure(Error.NotFound);
        entity.AltTextAr = Clean(input.AltTextAr);
        entity.AltTextEn = Clean(input.AltTextEn);
        Audit("Update", id, entity.OriginalFileName);
        return await CommitAsync(ct);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await Db.MediaFiles.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity is null) return Result.Failure(Error.NotFound);
        if (await IsInUseAsync(id, ct)) return Result.Failure(new Error("Media.InUse"));

        Db.MediaFiles.Remove(entity);
        Audit("Delete", id, entity.OriginalFileName);
        var result = await CommitAsync(ct);
        if (result.Succeeded) await storage.DeleteAsync(entity.StoragePath, entity.Kind, ct);
        return result;
    }

    /// <summary>Opens any media file (admin use; callers are responsible for authorization).</summary>
    public async Task<MediaDownload?> OpenAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await Db.MediaFiles.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity is null) return null;
        var stream = storage.OpenRead(entity.StoragePath, entity.Kind);
        return stream is null ? null : new MediaDownload(stream, entity.ContentType, entity.OriginalFileName);
    }

    /// <summary>Checks every content reference to a media file.</summary>
    public async Task<bool> IsInUseAsync(Guid id, CancellationToken ct = default) =>
        await Db.NewsArticles.AnyAsync(x => x.FeaturedImageId == id, ct)
        || await Db.Announcements.AnyAsync(x => x.AttachmentId == id, ct)
        || await Db.CenterServices.AnyAsync(x => x.CoverImageId == id, ct)
        || await Db.Reports.AnyAsync(x => x.PdfFileId == id || x.CoverImageId == id, ct)
        || await Db.TrainingCourses.AnyAsync(x => x.CourseImageId == id || x.AttachmentId == id, ct)
        || await Db.PhotoAlbums.AnyAsync(x => x.CoverMediaId == id, ct)
        || await Db.AlbumPhotos.AnyAsync(x => x.MediaFileId == id, ct)
        || await Db.Videos.AnyAsync(x => x.ThumbnailId == id, ct)
        || await Db.Leaders.AnyAsync(x => x.PhotoId == id, ct)
        || await Db.LegalDocuments.AnyAsync(x => x.DocumentFileId == id, ct)
        || await Db.ContentPages.AnyAsync(x => x.HeroImageId == id, ct)
        || await Db.PageSections.AnyAsync(x => x.ImageId == id, ct)
        || await Db.HeroSlides.AnyAsync(x => x.ImageId == id, ct)
        || await Db.HomeSections.AnyAsync(x => x.ImageId == id, ct);

    /// <summary>Verifies that <paramref name="id"/> (if set) refers to an existing media file of the given kind.</summary>
    public static async Task<bool> ExistsAsync(IApplicationDbContext db, Guid? id, MediaKind kind, CancellationToken ct) =>
        id is null || await db.MediaFiles.AnyAsync(m => m.Id == id && m.Kind == kind, ct);

    public static MediaFileDto ToDto(MediaFile m) => new(
        m.Id, m.OriginalFileName, m.ContentType, m.SizeBytes, m.Kind, m.AltTextAr, m.AltTextEn,
        m.Kind == MediaKind.Image ? MediaUrls.Image(m.StoragePath) : null, m.CreatedAtUtc);
}
