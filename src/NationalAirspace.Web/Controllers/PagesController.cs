using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using NationalAirspace.Application.Features.Pages;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Localization;
using NationalAirspace.Web.Services;
using NationalAirspace.Application.Common;

namespace NationalAirspace.Web.Controllers;

/// <summary>Airspace management, ATFM and admin-created custom pages.</summary>
[Route("{culture:culture}")]
public sealed class PagesController(PagePublicService pages) : PublicController
{
    [HttpGet("airspace-management")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> AirspaceManagement(CancellationToken ct)
    {
        var page = await pages.GetBySystemKeyAsync(SystemPages.AirspaceManagement, Lang, ct);
        if (page is null) return NotFound();
        Present(page);
        return View("Page", page);
    }

    [HttpGet("atfm")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Atfm(CancellationToken ct)
    {
        var page = await pages.GetBySystemKeyAsync(SystemPages.Atfm, Lang, ct);
        if (page is null) return NotFound();
        Present(page);
        return View(page);
    }

    [HttpGet("pages/{slug}")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Custom(string slug, CancellationToken ct)
    {
        var page = await pages.GetBySlugAsync(slug, Lang, ct);
        if (page is null || page.SystemKey is not null) return NotFound();
        if (RedirectToCanonical(slug, page.Slugs, "/pages") is { } redirect) return redirect;
        AlternatePath("/pages", page.Slugs);
        Present(page);
        return View("Page", page);
    }

    private void Present(PageDto page)
    {
        Seo(page.Seo.Title ?? page.Title, page.Seo.Description, page.HeroImage?.Url);
        ViewData["Lead"] = page.Summary;
        Breadcrumb((page.Title, null));
    }
}
