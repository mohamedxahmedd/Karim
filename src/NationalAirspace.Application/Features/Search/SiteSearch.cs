using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Search;

public enum SearchHitType { News, Service, Report, Training, Page, Announcement, Legislation }

/// <summary>A search result; <see cref="Key"/> is the slug (or id for announcements / legislation).</summary>
public sealed record SearchHit(SearchHitType Type, string Title, string? Snippet, string Key, DateTime? Date);

public sealed record SitemapEntry(SearchHitType Type, SlugPair Slugs, DateTime LastModifiedUtc);

public sealed class SiteSearchService(IApplicationDbContext db, IClock clock)
{
    private const int PerType = 8;

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(Lang lang, string? query, CancellationToken ct = default)
    {
        var term = query?.Trim();
        if (string.IsNullOrEmpty(term) || term.Length < 2) return [];
        if (term.Length > 100) term = term[..100];
        var now = clock.UtcNow;
        var hits = new List<SearchHit>();

        hits.AddRange((await db.NewsArticles.AsNoTracking().VisibleAt(now)
                .Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term) || (x.SummaryAr != null && x.SummaryAr.Contains(term)) || (x.SummaryEn != null && x.SummaryEn.Contains(term)))
                .OrderByDescending(x => x.PublishDateUtc).Take(PerType)
                .Select(x => new { x.TitleAr, x.TitleEn, x.SummaryAr, x.SummaryEn, x.SlugAr, x.SlugEn, x.PublishDateUtc }).ToListAsync(ct))
            .Select(x => new SearchHit(SearchHitType.News, lang.Pick(x.TitleAr, x.TitleEn), lang.PickOrNull(x.SummaryAr, x.SummaryEn), lang == Lang.Ar ? x.SlugAr : x.SlugEn, x.PublishDateUtc)));

        hits.AddRange((await db.CenterServices.AsNoTracking().Published()
                .Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term) || (x.ShortDescriptionAr != null && x.ShortDescriptionAr.Contains(term)) || (x.ShortDescriptionEn != null && x.ShortDescriptionEn.Contains(term)))
                .OrderBy(x => x.DisplayOrder).Take(PerType)
                .Select(x => new { x.TitleAr, x.TitleEn, x.ShortDescriptionAr, x.ShortDescriptionEn, x.SlugAr, x.SlugEn }).ToListAsync(ct))
            .Select(x => new SearchHit(SearchHitType.Service, lang.Pick(x.TitleAr, x.TitleEn), lang.PickOrNull(x.ShortDescriptionAr, x.ShortDescriptionEn), lang == Lang.Ar ? x.SlugAr : x.SlugEn, null)));

        hits.AddRange((await db.Reports.AsNoTracking().Published()
                .Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term) || (x.DescriptionAr != null && x.DescriptionAr.Contains(term)) || (x.DescriptionEn != null && x.DescriptionEn.Contains(term)))
                .OrderByDescending(x => x.PublicationDate).Take(PerType)
                .Select(x => new { x.TitleAr, x.TitleEn, x.DescriptionAr, x.DescriptionEn, x.SlugAr, x.SlugEn, x.PublicationDate }).ToListAsync(ct))
            .Select(x => new SearchHit(SearchHitType.Report, lang.Pick(x.TitleAr, x.TitleEn), lang.PickOrNull(x.DescriptionAr, x.DescriptionEn), lang == Lang.Ar ? x.SlugAr : x.SlugEn, x.PublicationDate)));

        hits.AddRange((await db.TrainingCourses.AsNoTracking().Published()
                .Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term))
                .OrderByDescending(x => x.StartDate).Take(PerType)
                .Select(x => new { x.TitleAr, x.TitleEn, x.TargetAudienceAr, x.TargetAudienceEn, x.SlugAr, x.SlugEn, x.StartDate }).ToListAsync(ct))
            .Select(x => new SearchHit(SearchHitType.Training, lang.Pick(x.TitleAr, x.TitleEn), lang.PickOrNull(x.TargetAudienceAr, x.TargetAudienceEn), lang == Lang.Ar ? x.SlugAr : x.SlugEn, x.StartDate)));

        hits.AddRange((await db.ContentPages.AsNoTracking().Published()
                .Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term) || (x.SummaryAr != null && x.SummaryAr.Contains(term)) || (x.SummaryEn != null && x.SummaryEn.Contains(term)))
                .Take(PerType)
                .Select(x => new { x.TitleAr, x.TitleEn, x.SummaryAr, x.SummaryEn, x.SlugAr, x.SlugEn, x.SystemKey }).ToListAsync(ct))
            .Select(x => new SearchHit(SearchHitType.Page, lang.Pick(x.TitleAr, x.TitleEn), lang.PickOrNull(x.SummaryAr, x.SummaryEn),
                x.SystemKey is null ? (lang == Lang.Ar ? x.SlugAr : x.SlugEn) : "@" + x.SystemKey, null)));

        hits.AddRange((await db.Announcements.AsNoTracking().ActiveAt(now)
                .Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term))
                .OrderByDescending(x => x.PublishDateUtc).Take(PerType)
                .Select(x => new { x.Id, x.TitleAr, x.TitleEn, x.PublishDateUtc }).ToListAsync(ct))
            .Select(x => new SearchHit(SearchHitType.Announcement, lang.Pick(x.TitleAr, x.TitleEn), null, x.Id.ToString(), x.PublishDateUtc)));

        hits.AddRange((await db.LegalDocuments.AsNoTracking().Published()
                .Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term) || (x.DocumentNumber != null && x.DocumentNumber.Contains(term)))
                .OrderByDescending(x => x.PublicationDate).Take(PerType)
                .Select(x => new { x.Id, x.TitleAr, x.TitleEn, x.DescriptionAr, x.DescriptionEn, x.PublicationDate }).ToListAsync(ct))
            .Select(x => new SearchHit(SearchHitType.Legislation, lang.Pick(x.TitleAr, x.TitleEn), lang.PickOrNull(x.DescriptionAr, x.DescriptionEn), x.Id.ToString(), x.PublicationDate)));

        return hits;
    }

    /// <summary>Slugged public content for sitemap.xml.</summary>
    public async Task<IReadOnlyList<SitemapEntry>> GetSitemapEntriesAsync(CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var entries = new List<SitemapEntry>();
        entries.AddRange((await db.NewsArticles.AsNoTracking().VisibleAt(now).Select(x => new { x.SlugAr, x.SlugEn, x.UpdatedAtUtc }).ToListAsync(ct))
            .Select(x => new SitemapEntry(SearchHitType.News, new SlugPair(x.SlugAr, x.SlugEn), x.UpdatedAtUtc)));
        entries.AddRange((await db.CenterServices.AsNoTracking().Published().Select(x => new { x.SlugAr, x.SlugEn, x.UpdatedAtUtc }).ToListAsync(ct))
            .Select(x => new SitemapEntry(SearchHitType.Service, new SlugPair(x.SlugAr, x.SlugEn), x.UpdatedAtUtc)));
        entries.AddRange((await db.Reports.AsNoTracking().Published().Select(x => new { x.SlugAr, x.SlugEn, x.UpdatedAtUtc }).ToListAsync(ct))
            .Select(x => new SitemapEntry(SearchHitType.Report, new SlugPair(x.SlugAr, x.SlugEn), x.UpdatedAtUtc)));
        entries.AddRange((await db.TrainingCourses.AsNoTracking().Published().Select(x => new { x.SlugAr, x.SlugEn, x.UpdatedAtUtc }).ToListAsync(ct))
            .Select(x => new SitemapEntry(SearchHitType.Training, new SlugPair(x.SlugAr, x.SlugEn), x.UpdatedAtUtc)));
        entries.AddRange((await db.ContentPages.AsNoTracking().Published().Where(p => p.SystemKey == null).Select(x => new { x.SlugAr, x.SlugEn, x.UpdatedAtUtc }).ToListAsync(ct))
            .Select(x => new SitemapEntry(SearchHitType.Page, new SlugPair(x.SlugAr, x.SlugEn), x.UpdatedAtUtc)));
        return entries;
    }
}
