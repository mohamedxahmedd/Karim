using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.News;

public sealed class NewsPublicService(IApplicationDbContext db, IClock clock)
{
    private IQueryable<NewsArticle> Visible() =>
        db.NewsArticles.AsNoTracking().VisibleAt(clock.UtcNow).Include(x => x.FeaturedImage).Include(x => x.NewsCategory);

    public async Task<IReadOnlyList<NewsCardDto>> GetLatestAsync(Lang lang, int count, CancellationToken ct = default)
    {
        var items = await Visible().OrderByDescending(x => x.IsFeatured).ThenByDescending(x => x.PublishDateUtc)
            .Take(Math.Clamp(count, 1, 24)).ToListAsync(ct);
        return items.Select(x => ToCard(x, lang)).ToList();
    }

    public async Task<PagedResult<NewsCardDto>> SearchAsync(Lang lang, string? search, string? categorySlug, int page, int pageSize = 9, CancellationToken ct = default)
    {
        var query = Visible();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term)
                                  || (x.SummaryAr != null && x.SummaryAr.Contains(term))
                                  || (x.SummaryEn != null && x.SummaryEn.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(categorySlug))
            query = query.Where(x => x.NewsCategory != null && x.NewsCategory.Slug == categorySlug && x.NewsCategory.IsActive);
        var paged = await query.OrderByDescending(x => x.PublishDateUtc).ToPagedAsync(page, pageSize, ct);
        return paged.Map(x => ToCard(x, lang));
    }

    public async Task<NewsDetailDto?> GetBySlugAsync(Lang lang, string slug, CancellationToken ct = default)
    {
        var x = await Visible().FirstOrDefaultAsync(n => n.SlugAr == slug || n.SlugEn == slug, ct);
        return x is null ? null : await ToDetailAsync(x, lang, ct);
    }

    /// <summary>Admin preview: returns the article regardless of status.</summary>
    public async Task<NewsDetailDto?> GetForPreviewAsync(Lang lang, Guid id, CancellationToken ct = default)
    {
        var x = await db.NewsArticles.AsNoTracking().Include(n => n.FeaturedImage).Include(n => n.NewsCategory)
            .FirstOrDefaultAsync(n => n.Id == id, ct);
        return x is null ? null : await ToDetailAsync(x, lang, ct);
    }

    public async Task<IReadOnlyList<NewsCategoryDto>> GetCategoriesAsync(Lang lang, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var cats = await db.NewsCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.DisplayOrder)
            .Select(c => new { c.NameAr, c.NameEn, c.Slug, Count = c.Articles.Count(a => a.Status == ContentStatus.Published && a.PublishDateUtc != null && a.PublishDateUtc <= now) })
            .ToListAsync(ct);
        return cats.Where(c => c.Count > 0).Select(c => new NewsCategoryDto(lang.Pick(c.NameAr, c.NameEn), c.Slug, c.Count)).ToList();
    }

    public async Task<string?> GetCategoryNameAsync(Lang lang, string slug, CancellationToken ct = default)
    {
        var c = await db.NewsCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, ct);
        return c is null ? null : lang.Pick(c.NameAr, c.NameEn);
    }

    private async Task<NewsDetailDto> ToDetailAsync(NewsArticle x, Lang lang, CancellationToken ct)
    {
        var related = await Visible()
            .Where(n => n.Id != x.Id && (x.NewsCategoryId == null || n.NewsCategoryId == x.NewsCategoryId))
            .OrderByDescending(n => n.PublishDateUtc).Take(3).ToListAsync(ct);
        var title = lang.Pick(x.TitleAr, x.TitleEn);
        return new NewsDetailDto(
            x.Id, title, lang.PickOrNull(x.SummaryAr, x.SummaryEn), lang.PickOrNull(x.ContentAr, x.ContentEn),
            new SlugPair(x.SlugAr, x.SlugEn), ImageRef.From(x.FeaturedImage, lang, title),
            x.PublishDateUtc ?? x.CreatedAtUtc, lang.PickOrNull(x.AuthorAr, x.AuthorEn),
            x.NewsCategory is null ? null : lang.Pick(x.NewsCategory.NameAr, x.NewsCategory.NameEn), x.NewsCategory?.Slug,
            SeoMeta.From(lang, x.SeoTitleAr, x.SeoTitleEn, x.MetaDescriptionAr ?? x.SummaryAr, x.MetaDescriptionEn ?? x.SummaryEn),
            related.Select(r => ToCard(r, lang)).ToList());
    }

    internal static NewsCardDto ToCard(NewsArticle x, Lang lang)
    {
        var title = lang.Pick(x.TitleAr, x.TitleEn);
        return new NewsCardDto(x.Id, title, lang.PickOrNull(x.SummaryAr, x.SummaryEn),
            lang == Lang.Ar ? x.SlugAr : x.SlugEn, ImageRef.From(x.FeaturedImage, lang, title),
            x.PublishDateUtc ?? x.CreatedAtUtc,
            x.NewsCategory is null ? null : lang.Pick(x.NewsCategory.NameAr, x.NewsCategory.NameEn), x.NewsCategory?.Slug, x.IsFeatured);
    }
}
