using NationalAirspace.Domain.Common;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Common;

/// <summary>Public-visibility filters, shared by every public query so the rules live in one place.</summary>
public static class ContentQueries
{
    public static IQueryable<T> Published<T>(this IQueryable<T> query) where T : PublishableEntity =>
        query.Where(x => x.Status == ContentStatus.Published);

    /// <summary>Published and the publication instant has been reached (scheduled items stay hidden).</summary>
    public static IQueryable<T> VisibleAt<T>(this IQueryable<T> query, DateTime nowUtc) where T : ScheduledEntity =>
        query.Where(x => x.Status == ContentStatus.Published && x.PublishDateUtc != null && x.PublishDateUtc <= nowUtc);

    /// <summary>Visible announcements that have not expired.</summary>
    public static IQueryable<Announcement> ActiveAt(this IQueryable<Announcement> query, DateTime nowUtc) =>
        query.VisibleAt(nowUtc).Where(x => x.ExpiresAtUtc == null || x.ExpiresAtUtc > nowUtc);
}

/// <summary>A localized image reference for public views.</summary>
public sealed record ImageRef(string Url, string Alt)
{
    public static ImageRef? From(MediaFile? media, Lang lang, string fallbackAlt) =>
        media is null || media.Kind != MediaKind.Image
            ? null
            : new ImageRef(MediaUrls.Image(media.StoragePath)!, lang.PickOrNull(media.AltTextAr, media.AltTextEn) ?? fallbackAlt);
}

/// <summary>Localized SEO metadata for a page.</summary>
public sealed record SeoMeta(string? Title, string? Description)
{
    public static SeoMeta From(Lang lang, string? titleAr, string? titleEn, string? descAr, string? descEn) =>
        new(lang.PickOrNull(titleAr, titleEn), lang.PickOrNull(descAr, descEn));
}

/// <summary>Both-language slugs, used for canonical redirects and the language switcher.</summary>
public sealed record SlugPair(string Ar, string En)
{
    public string For(Lang lang) => lang == Lang.Ar ? Ar : En;
}
