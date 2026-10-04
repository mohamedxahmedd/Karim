using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Application.Features.News;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Reports;

public class ReportInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleEn { get; set; } = string.Empty;
    [StringLength(4000, ErrorMessage = V.MaxLength)] public string? DescriptionAr { get; set; }
    [StringLength(4000, ErrorMessage = V.MaxLength)] public string? DescriptionEn { get; set; }
    [Slug, StringLength(150, ErrorMessage = V.MaxLength)] public string? SlugAr { get; set; }
    [Slug, StringLength(150, ErrorMessage = V.MaxLength)] public string? SlugEn { get; set; }
    public Guid? ReportCategoryId { get; set; }
    [Required(ErrorMessage = V.Required), DataType(DataType.Date)] public DateTime? PublicationDate { get; set; }
    public Guid? PdfFileId { get; set; }
    public Guid? CoverImageId { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    [StringLength(200, ErrorMessage = V.MaxLength)] public string? SeoTitleAr { get; set; }
    [StringLength(200, ErrorMessage = V.MaxLength)] public string? SeoTitleEn { get; set; }
    [StringLength(400, ErrorMessage = V.MaxLength)] public string? MetaDescriptionAr { get; set; }
    [StringLength(400, ErrorMessage = V.MaxLength)] public string? MetaDescriptionEn { get; set; }
}

public sealed record ReportAdminListItem(Guid Id, string TitleAr, string TitleEn, ContentStatus Status, DateTime PublicationDate,
    string? CategoryAr, string? CategoryEn, bool HasFile, int DownloadCount, DateTime UpdatedAtUtc);

public sealed record ReportCardDto(Guid Id, string Title, string? Description, string Slug, DateTime PublicationDate,
    string? CategoryName, string? CategorySlug, ImageRef? Cover, bool HasFile, long? FileSize);

public sealed record ReportDetailDto(Guid Id, string Title, string? Description, SlugPair Slugs, DateTime PublicationDate,
    string? CategoryName, string? CategorySlug, ImageRef? Cover, string? FileName, long? FileSize, string? FileType, SeoMeta Seo);

public sealed class ReportAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "Report";

    public async Task<PagedResult<ReportAdminListItem>> ListAsync(AdminListQuery q, CancellationToken ct = default)
    {
        var query = Db.Reports.AsNoTracking();
        if (q.Term is { } term) query = query.Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term));
        if (q.Status is { } status) query = query.Where(x => x.Status == status);
        if (q.CategoryId is { } cat) query = query.Where(x => x.ReportCategoryId == cat);
        return await query.OrderByDescending(x => x.PublicationDate)
            .Select(x => new ReportAdminListItem(x.Id, x.TitleAr, x.TitleEn, x.Status, x.PublicationDate,
                x.ReportCategory != null ? x.ReportCategory.NameAr : null, x.ReportCategory != null ? x.ReportCategory.NameEn : null,
                x.PdfFileId != null, x.DownloadCount, x.UpdatedAtUtc))
            .ToPagedAsync(q.Page, q.PageSize, ct);
    }

    public async Task<ReportInput?> GetForEditAsync(Guid id, CancellationToken ct = default) =>
        await Db.Reports.AsNoTracking().Where(x => x.Id == id).Select(x => new ReportInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, TitleAr = x.TitleAr, TitleEn = x.TitleEn,
            DescriptionAr = x.DescriptionAr, DescriptionEn = x.DescriptionEn, SlugAr = x.SlugAr, SlugEn = x.SlugEn,
            ReportCategoryId = x.ReportCategoryId, PublicationDate = x.PublicationDate, PdfFileId = x.PdfFileId,
            CoverImageId = x.CoverImageId, Status = x.Status,
            SeoTitleAr = x.SeoTitleAr, SeoTitleEn = x.SeoTitleEn, MetaDescriptionAr = x.MetaDescriptionAr, MetaDescriptionEn = x.MetaDescriptionEn,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveAsync(ReportInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        var entity = input.Id is { } id ? await Db.Reports.FirstOrDefaultAsync(x => x.Id == id, ct) : new Report();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        if (input.ReportCategoryId is { } catId && !await Db.ReportCategories.AnyAsync(c => c.Id == catId, ct))
            errors.Add(new Error("Val.InvalidOption", nameof(input.ReportCategoryId)));
        if (!await MediaService.ExistsAsync(Db, input.PdfFileId, MediaKind.Document, ct))
            errors.Add(new Error("Val.InvalidOption", nameof(input.PdfFileId)));
        if (!await MediaService.ExistsAsync(Db, input.CoverImageId, MediaKind.Image, ct))
            errors.Add(new Error("Val.InvalidOption", nameof(input.CoverImageId)));
        if (input.Status == ContentStatus.Published && input.PdfFileId is null)
            errors.Add(new Error("Reports.FileRequiredToPublish", nameof(input.PdfFileId)));
        var slugs = await ResolveSlugsAsync(Db.Reports, entity.Id, input.SlugAr, input.SlugEn, input.TitleAr, input.TitleEn, errors, ct);
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.DescriptionAr = Clean(input.DescriptionAr);
        entity.DescriptionEn = Clean(input.DescriptionEn);
        entity.SlugAr = slugs.Ar;
        entity.SlugEn = slugs.En;
        entity.ReportCategoryId = input.ReportCategoryId;
        entity.PublicationDate = input.PublicationDate!.Value.AsUtcDate();
        entity.PdfFileId = input.PdfFileId;
        entity.CoverImageId = input.CoverImageId;
        entity.SeoTitleAr = Clean(input.SeoTitleAr);
        entity.SeoTitleEn = Clean(input.SeoTitleEn);
        entity.MetaDescriptionAr = Clean(input.MetaDescriptionAr);
        entity.MetaDescriptionEn = Clean(input.MetaDescriptionEn);
        entity.ChangeStatus(input.Status, Clock.UtcNow);

        if (input.Id is null)
        {
            Db.Reports.Add(entity);
            Audit("Create", entity.Id, entity.TitleAr);
        }
        else
        {
            ExpectStamp(entity, input.ConcurrencyStamp);
            Audit("Update", entity.Id, entity.TitleAr);
        }
        return await CommitAsync(entity.Id, ct);
    }

    public async Task<Result> ChangeStatusAsync(Guid id, ContentStatus status, CancellationToken ct = default)
    {
        if (status == ContentStatus.Published && await Db.Reports.AnyAsync(r => r.Id == id && r.PdfFileId == null, ct))
            return Result.Failure(new Error("Reports.FileRequiredToPublish"));
        return await ChangeStatusAsync(Db.Reports, id, status, x => x.TitleAr, ct);
    }

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync(Db.Reports, id, x => x.TitleAr, ct);

    public async Task<IReadOnlyList<CategoryOption>> GetCategoryOptionsAsync(CancellationToken ct = default) =>
        await Db.ReportCategories.AsNoTracking().OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryOption(c.Id, c.NameAr, c.NameEn)).ToListAsync(ct);
}

public sealed class ReportCategoryAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "ReportCategory";

    public async Task<IReadOnlyList<CategoryAdminItem>> ListAsync(CancellationToken ct = default) =>
        await Db.ReportCategories.AsNoTracking().OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryAdminItem(c.Id, c.NameAr, c.NameEn, c.Slug, c.IsActive, c.DisplayOrder, c.Reports.Count))
            .ToListAsync(ct);

    public async Task<NewsCategoryInput?> GetForEditAsync(Guid id, CancellationToken ct = default) =>
        await Db.ReportCategories.AsNoTracking().Where(c => c.Id == id)
            .Select(c => new NewsCategoryInput { Id = c.Id, ConcurrencyStamp = c.ConcurrencyStamp, NameAr = c.NameAr, NameEn = c.NameEn, Slug = c.Slug, IsActive = c.IsActive })
            .FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveAsync(NewsCategoryInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        var entity = input.Id is { } id ? await Db.ReportCategories.FirstOrDefaultAsync(c => c.Id == id, ct) : new ReportCategory();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        var slug = string.IsNullOrWhiteSpace(input.Slug) ? SlugGenerator.Generate(input.NameEn) : input.Slug.Trim().ToLowerInvariant();
        if (slug.Length == 0) slug = SlugGenerator.Generate(input.NameAr);
        if (await Db.ReportCategories.AnyAsync(c => c.Id != entity.Id && c.Slug == slug, ct))
            errors.Add(new Error(V.SlugTaken, nameof(input.Slug)));
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        entity.NameAr = input.NameAr.Trim();
        entity.NameEn = input.NameEn.Trim();
        entity.Slug = slug;
        entity.IsActive = input.IsActive;
        if (input.Id is null)
        {
            entity.DisplayOrder = await NextOrderAsync(Db.ReportCategories, ct);
            Db.ReportCategories.Add(entity);
            Audit("Create", entity.Id, entity.NameAr);
        }
        else
        {
            ExpectStamp(entity, input.ConcurrencyStamp);
            Audit("Update", entity.Id, entity.NameAr);
        }
        return await CommitAsync(entity.Id, ct);
    }

    public Task<Result> MoveAsync(Guid id, int direction, CancellationToken ct = default) =>
        MoveAsync(Db.ReportCategories, id, direction, ct);

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await Db.Reports.Where(r => r.ReportCategoryId == id).LoadAsync(ct);
        return await DeleteAsync(Db.ReportCategories, id, x => x.NameAr, ct);
    }
}

public sealed class ReportPublicService(IApplicationDbContext db)
{
    private IQueryable<Report> Visible() => db.Reports.AsNoTracking().Published()
        .Include(r => r.ReportCategory).Include(r => r.CoverImage).Include(r => r.PdfFile);

    public async Task<IReadOnlyList<ReportCardDto>> GetLatestAsync(Lang lang, int count, CancellationToken ct = default) =>
        (await Visible().OrderByDescending(r => r.PublicationDate).Take(Math.Clamp(count, 1, 24)).ToListAsync(ct))
        .Select(r => ToCard(r, lang)).ToList();

    public async Task<PagedResult<ReportCardDto>> SearchAsync(Lang lang, string? search, string? categorySlug, int? year, int page, int pageSize = 9, CancellationToken ct = default)
    {
        var query = Visible();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r => r.TitleAr.Contains(term) || r.TitleEn.Contains(term)
                                  || (r.DescriptionAr != null && r.DescriptionAr.Contains(term))
                                  || (r.DescriptionEn != null && r.DescriptionEn.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(categorySlug)) query = query.Where(r => r.ReportCategory != null && r.ReportCategory.Slug == categorySlug);
        if (year is { } y)
        {
            var from = new DateTime(y, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            query = query.Where(r => r.PublicationDate >= from && r.PublicationDate < from.AddYears(1));
        }
        var paged = await query.OrderByDescending(r => r.PublicationDate).ToPagedAsync(page, pageSize, ct);
        return paged.Map(r => ToCard(r, lang));
    }

    public async Task<IReadOnlyList<NewsCategoryDto>> GetCategoriesAsync(Lang lang, CancellationToken ct = default) =>
        (await db.ReportCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.DisplayOrder)
            .Select(c => new { c.NameAr, c.NameEn, c.Slug, Count = c.Reports.Count(r => r.Status == ContentStatus.Published) })
            .ToListAsync(ct))
        .Where(c => c.Count > 0).Select(c => new NewsCategoryDto(lang.Pick(c.NameAr, c.NameEn), c.Slug, c.Count)).ToList();

    public async Task<IReadOnlyList<int>> GetYearsAsync(CancellationToken ct = default) =>
        (await db.Reports.AsNoTracking().Published().Select(r => r.PublicationDate).ToListAsync(ct))
        .Select(d => d.Year).Distinct().OrderByDescending(y => y).ToList();

    public async Task<ReportDetailDto?> GetBySlugAsync(Lang lang, string slug, CancellationToken ct = default)
    {
        var r = await Visible().FirstOrDefaultAsync(x => x.SlugAr == slug || x.SlugEn == slug, ct);
        if (r is null) return null;
        var title = lang.Pick(r.TitleAr, r.TitleEn);
        return new ReportDetailDto(r.Id, title, lang.PickOrNull(r.DescriptionAr, r.DescriptionEn), new SlugPair(r.SlugAr, r.SlugEn),
            r.PublicationDate, r.ReportCategory is null ? null : lang.Pick(r.ReportCategory.NameAr, r.ReportCategory.NameEn),
            r.ReportCategory?.Slug, ImageRef.From(r.CoverImage, lang, title), r.PdfFile?.OriginalFileName, r.PdfFile?.SizeBytes,
            r.PdfFile is null ? null : Path.GetExtension(r.PdfFile.OriginalFileName).TrimStart('.').ToUpperInvariant(),
            SeoMeta.From(lang, r.SeoTitleAr, r.SeoTitleEn, r.MetaDescriptionAr ?? r.DescriptionAr, r.MetaDescriptionEn ?? r.DescriptionEn));
    }

    /// <summary>Returns the file of a published report and counts the download.</summary>
    public async Task<Guid?> RegisterDownloadAsync(string slug, CancellationToken ct = default)
    {
        var report = await db.Reports.AsNoTracking().Published()
            .Where(x => (x.SlugAr == slug || x.SlugEn == slug) && x.PdfFileId != null)
            .Select(x => new { x.Id, x.PdfFileId }).FirstOrDefaultAsync(ct);
        if (report is null) return null;
        // Set-based update: does not touch UpdatedAtUtc/ConcurrencyStamp, so editors are not disturbed.
        await db.Reports.Where(x => x.Id == report.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DownloadCount, x => x.DownloadCount + 1), ct);
        return report.PdfFileId;
    }

    private static ReportCardDto ToCard(Report r, Lang lang)
    {
        var title = lang.Pick(r.TitleAr, r.TitleEn);
        return new ReportCardDto(r.Id, title, lang.PickOrNull(r.DescriptionAr, r.DescriptionEn), lang == Lang.Ar ? r.SlugAr : r.SlugEn,
            r.PublicationDate, r.ReportCategory is null ? null : lang.Pick(r.ReportCategory.NameAr, r.ReportCategory.NameEn),
            r.ReportCategory?.Slug, ImageRef.From(r.CoverImage, lang, title), r.PdfFileId != null, r.PdfFile?.SizeBytes);
    }
}
