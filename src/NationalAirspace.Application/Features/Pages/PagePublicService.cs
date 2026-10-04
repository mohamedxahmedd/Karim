using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Pages;

public sealed class PagePublicService(IApplicationDbContext db)
{
    private IQueryable<ContentPage> WithSections() => db.ContentPages.AsNoTracking()
        .Include(p => p.HeroImage)
        .Include(p => p.Sections.Where(s => s.IsVisible).OrderBy(s => s.DisplayOrder)).ThenInclude(s => s.Image);

    public async Task<PageDto?> GetBySystemKeyAsync(string systemKey, Lang lang, CancellationToken ct = default)
    {
        var page = await WithSections().Published().FirstOrDefaultAsync(p => p.SystemKey == systemKey, ct);
        return page is null ? null : ToDto(page, lang);
    }

    public async Task<PageDto?> GetBySlugAsync(string slug, Lang lang, CancellationToken ct = default)
    {
        var page = await WithSections().Published().FirstOrDefaultAsync(p => p.SlugAr == slug || p.SlugEn == slug, ct);
        return page is null ? null : ToDto(page, lang);
    }

    /// <summary>Admin preview (any status).</summary>
    public async Task<PageDto?> GetForPreviewAsync(Guid id, Lang lang, CancellationToken ct = default)
    {
        var page = await WithSections().FirstOrDefaultAsync(p => p.Id == id, ct);
        return page is null ? null : ToDto(page, lang);
    }

    /// <summary>Visible sections of a published system page, optionally filtered by layout.</summary>
    public async Task<IReadOnlyList<PageSectionDto>> GetSectionsAsync(string systemKey, Lang lang, PageSectionLayout? layout = null, CancellationToken ct = default)
    {
        var query = db.PageSections.AsNoTracking().Include(s => s.Image)
            .Where(s => s.IsVisible && s.Page!.SystemKey == systemKey && s.Page.Status == ContentStatus.Published);
        if (layout is { } l) query = query.Where(s => s.Layout == l);
        var sections = await query.OrderBy(s => s.DisplayOrder).ToListAsync(ct);
        return sections.Select(s => ToSection(s, lang)).ToList();
    }

    public async Task<IReadOnlyList<(SlugPair Slugs, DateTime UpdatedAtUtc)>> GetCustomPageSlugsAsync(CancellationToken ct = default) =>
        (await db.ContentPages.AsNoTracking().Published().Where(p => p.SystemKey == null)
            .Select(p => new { p.SlugAr, p.SlugEn, p.UpdatedAtUtc }).ToListAsync(ct))
        .Select(p => (new SlugPair(p.SlugAr, p.SlugEn), p.UpdatedAtUtc)).ToList();

    private static PageDto ToDto(ContentPage p, Lang lang)
    {
        var title = lang.Pick(p.TitleAr, p.TitleEn);
        return new PageDto(p.Id, p.SystemKey, title, lang.PickOrNull(p.SummaryAr, p.SummaryEn), lang.PickOrNull(p.ContentAr, p.ContentEn),
            ImageRef.From(p.HeroImage, lang, title), new SlugPair(p.SlugAr, p.SlugEn),
            SeoMeta.From(lang, p.SeoTitleAr, p.SeoTitleEn, p.MetaDescriptionAr ?? p.SummaryAr, p.MetaDescriptionEn ?? p.SummaryEn),
            p.Sections.OrderBy(s => s.DisplayOrder).Select(s => ToSection(s, lang)).ToList(), p.UpdatedAtUtc);
    }

    private static PageSectionDto ToSection(PageSection s, Lang lang)
    {
        var title = lang.Pick(s.TitleAr, s.TitleEn);
        return new PageSectionDto(s.Key, s.Layout, title, lang.PickOrNull(s.ContentAr, s.ContentEn), s.Icon, ImageRef.From(s.Image, lang, title));
    }
}
