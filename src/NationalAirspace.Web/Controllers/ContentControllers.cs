using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using NationalAirspace.Application.Features.Announcements;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Application.Features.MediaCenter;
using NationalAirspace.Application.Features.News;
using NationalAirspace.Application.Features.Reports;
using NationalAirspace.Application.Features.Search;
using NationalAirspace.Application.Features.Services;
using NationalAirspace.Application.Features.Training;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Localization;
using NationalAirspace.Web.ViewModels;

namespace NationalAirspace.Web.Controllers;

[Route("{culture:culture}/services")]
public sealed class ServicesController(ServicePublicService services, IStringLocalizer<SharedResource> L) : PublicController
{
    [HttpGet("")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        Seo(L["Nav.Services"], L["Services.Lead"]);
        ViewData["Lead"] = L["Services.Lead"].Value;
        Breadcrumb((L["Nav.Services"], null));
        return View(await services.GetAllAsync(Lang, ct: ct));
    }

    [HttpGet("{slug}")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var item = await services.GetBySlugAsync(Lang, slug, ct);
        if (item is null) return NotFound();
        if (RedirectToCanonical(slug, item.Slugs, "/services") is { } redirect) return redirect;
        AlternatePath("/services", item.Slugs);
        Seo(item.Seo.Title ?? item.Title, item.Seo.Description, item.Image?.Url);
        ViewData["Lead"] = item.ShortDescription;
        Breadcrumb((L["Nav.Services"], Url.Site("/services")), (item.Title, null));
        return View(item);
    }
}

[Route("{culture:culture}/news")]
public sealed class NewsController(NewsPublicService news, IStringLocalizer<SharedResource> L) : PublicController
{
    [HttpGet("")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Index(string? q, string? category, int page = 1, CancellationToken ct = default)
    {
        var categoryName = string.IsNullOrEmpty(category) ? null : await news.GetCategoryNameAsync(Lang, category, ct);
        Seo(categoryName ?? L["Nav.News"], L["News.Lead"]);
        ViewData["Lead"] = L["News.Lead"].Value;
        Breadcrumb(categoryName is null ? [(L["Nav.News"], null)] : [(L["Nav.News"], Url.Site("/news")), (categoryName, null)]);
        var result = await news.SearchAsync(Lang, q, category, page, ct: ct);
        return View(new NewsListModel(result, q, category, await news.GetCategoriesAsync(Lang, ct)));
    }

    [HttpGet("{slug}")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var item = await news.GetBySlugAsync(Lang, slug, ct);
        if (item is null) return NotFound();
        if (RedirectToCanonical(slug, item.Slugs, "/news") is { } redirect) return redirect;
        AlternatePath("/news", item.Slugs);
        Seo(item.Seo.Title ?? item.Title, item.Seo.Description, item.Image?.Url);
        Breadcrumb((L["Nav.News"], Url.Site("/news")), (item.Title, null));
        return View(item);
    }
}

[Route("{culture:culture}/announcements")]
public sealed class AnnouncementsController(AnnouncementPublicService announcements, IStringLocalizer<SharedResource> L) : PublicController
{
    [HttpGet("")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Index(string? tab, int page = 1, CancellationToken ct = default)
    {
        var archive = string.Equals(tab, "archive", StringComparison.OrdinalIgnoreCase);
        Seo(L["Nav.Announcements"], L["Announcements.Lead"]);
        ViewData["Lead"] = L["Announcements.Lead"].Value;
        Breadcrumb((L["Nav.Announcements"], null));
        var result = archive ? await announcements.GetArchiveAsync(Lang, page, ct: ct) : await announcements.GetActiveAsync(Lang, page, ct: ct);
        return View(new ListPageModel<AnnouncementDto>(result, null) { Tab = archive ? "archive" : null });
    }

    [HttpGet("{id:guid}")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var item = await announcements.GetAsync(Lang, id, ct);
        if (item is null) return NotFound();
        Seo(item.Title);
        Breadcrumb((L["Nav.Announcements"], Url.Site("/announcements")), (item.Title, null));
        return View(item);
    }

    [HttpGet("{id:guid}/attachment")]
    public async Task<IActionResult> Attachment(Guid id, [FromServices] MediaService media, CancellationToken ct) =>
        await DownloadHelper.FileAsync(this, media, await announcements.GetAttachmentIdAsync(id, ct), ct);
}

[Route("{culture:culture}/reports")]
public sealed class ReportsController(ReportPublicService reports, IStringLocalizer<SharedResource> L) : PublicController
{
    [HttpGet("")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Index(string? q, string? category, int? year, int page = 1, CancellationToken ct = default)
    {
        Seo(L["Nav.Reports"], L["Reports.Lead"]);
        ViewData["Lead"] = L["Reports.Lead"].Value;
        Breadcrumb((L["Nav.Reports"], null));
        var result = await reports.SearchAsync(Lang, q, category, year, page, ct: ct);
        return View(new ReportListModel(result, q, category, year, await reports.GetCategoriesAsync(Lang, ct), await reports.GetYearsAsync(ct)));
    }

    [HttpGet("{slug}")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var item = await reports.GetBySlugAsync(Lang, slug, ct);
        if (item is null) return NotFound();
        if (RedirectToCanonical(slug, item.Slugs, "/reports") is { } redirect) return redirect;
        AlternatePath("/reports", item.Slugs);
        Seo(item.Seo.Title ?? item.Title, item.Seo.Description, item.Cover?.Url);
        Breadcrumb((L["Nav.Reports"], Url.Site("/reports")), (item.Title, null));
        return View(item);
    }

    [HttpGet("{slug}/download")]
    public async Task<IActionResult> Download(string slug, [FromServices] MediaService media, CancellationToken ct) =>
        await DownloadHelper.FileAsync(this, media, await reports.RegisterDownloadAsync(slug, ct), ct);
}

[Route("{culture:culture}/training")]
public sealed class TrainingController(TrainingPublicService training, IStringLocalizer<SharedResource> L) : PublicController
{
    [HttpGet("")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Index(string? q, string? tab, int page = 1, CancellationToken ct = default)
    {
        var past = string.Equals(tab, "past", StringComparison.OrdinalIgnoreCase);
        Seo(L["Nav.Training"], L["Training.Lead"]);
        ViewData["Lead"] = L["Training.Lead"].Value;
        Breadcrumb((L["Nav.Training"], null));
        var result = await training.SearchAsync(Lang, q, past, page, ct: ct);
        return View(new ListPageModel<TrainingCardDto>(result, q) { Tab = past ? "past" : null });
    }

    [HttpGet("{slug}")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Details(string slug, CancellationToken ct)
    {
        var item = await training.GetBySlugAsync(Lang, slug, ct);
        if (item is null) return NotFound();
        if (RedirectToCanonical(slug, item.Slugs, "/training") is { } redirect) return redirect;
        AlternatePath("/training", item.Slugs);
        Seo(item.Title, null, item.Image?.Url);
        Breadcrumb((L["Nav.Training"], Url.Site("/training")), (item.Title, null));
        return View(item);
    }

    [HttpGet("{slug}/attachment")]
    public async Task<IActionResult> Attachment(string slug, [FromServices] MediaService media, CancellationToken ct) =>
        await DownloadHelper.FileAsync(this, media, await training.GetAttachmentIdAsync(slug, ct), ct);
}

[Route("{culture:culture}/media")]
public sealed class MediaCenterController(AlbumPublicService albums, VideoPublicService videos, IStringLocalizer<SharedResource> L) : PublicController
{
    [HttpGet("")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        Seo(L["Nav.MediaCenter"], L["Media.Lead"]);
        ViewData["Lead"] = L["Media.Lead"].Value;
        Breadcrumb((L["Nav.MediaCenter"], null));
        return View(new MediaCenterModel(await albums.ListAsync(Lang, 1, 6, ct), await videos.ListAsync(Lang, 1, 3, ct)));
    }

    [HttpGet("photos")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Photos(int page = 1, CancellationToken ct = default)
    {
        Seo(L["Nav.Photos"]);
        Breadcrumb((L["Nav.MediaCenter"], Url.Site("/media")), (L["Nav.Photos"], null));
        return View(await albums.ListAsync(Lang, page, ct: ct));
    }

    [HttpGet("photos/{id:guid}")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Album(Guid id, CancellationToken ct)
    {
        var album = await albums.GetAsync(Lang, id, ct);
        if (album is null) return NotFound();
        Seo(album.Title, album.Description, album.Photos.FirstOrDefault()?.Url);
        ViewData["Lead"] = album.Description;
        Breadcrumb((L["Nav.MediaCenter"], Url.Site("/media")), (L["Nav.Photos"], Url.Site("/media/photos")), (album.Title, null));
        return View(album);
    }

    [HttpGet("videos")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Videos(int page = 1, CancellationToken ct = default)
    {
        Seo(L["Nav.Videos"]);
        Breadcrumb((L["Nav.MediaCenter"], Url.Site("/media")), (L["Nav.Videos"], null));
        return View(await videos.ListAsync(Lang, page, ct: ct));
    }
}

[Route("{culture:culture}/search")]
public sealed class SearchController(SiteSearchService search, IStringLocalizer<SharedResource> L) : PublicController
{
    [HttpGet("")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        Seo(L["Search.Title"]);
        ViewData["NoIndex"] = true;
        Breadcrumb((L["Search.Title"], null));
        return View(new SearchModel(q?.Trim(), await search.SearchAsync(Lang, q, ct)));
    }
}
