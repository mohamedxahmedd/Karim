using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using NationalAirspace.Application.Features.DemoAirspace;
using NationalAirspace.Application.Features.Pages;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Localization;

namespace NationalAirspace.Web.Controllers;

/// <summary>Airspace management, ATFM, the demo dashboard and admin-created custom pages.</summary>
[Route("{culture:culture}")]
public sealed class PagesController(PagePublicService pages, IStringLocalizer<SharedResource> L) : PublicController
{
    [HttpGet("airspace-management")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> AirspaceManagement(CancellationToken ct)
    {
        var page = await pages.GetBySystemKeyAsync(SystemPages.AirspaceManagement, Lang, ct);
        if (page is null) return NotFound();
        Present(page);
        ViewData["ShowDemoLink"] = true;
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

    [HttpGet("airspace-demo")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public IActionResult Demo([FromServices] DemoAirspaceService demo)
    {
        Seo(L["Demo.Title"], L["Demo.Lead"]);
        ViewData["Lead"] = L["Demo.Lead"].Value;
        Breadcrumb((L["Nav.AirspaceManagement"], Url.Site("/airspace-management")), (L["Demo.Title"], null));
        return View(demo.GetSnapshot(Lang));
    }

    /// <summary>Simulated snapshot (JSON) used by the demo dashboard to refresh itself.</summary>
    [HttpGet("airspace-demo/data")]
    [OutputCache(Duration = 15)]
    public IActionResult DemoData([FromServices] DemoAirspaceService demo) => Json(demo.GetSnapshot(Lang));

    private void Present(PageDto page)
    {
        Seo(page.Seo.Title ?? page.Title, page.Seo.Description, page.HeroImage?.Url);
        ViewData["Lead"] = page.Summary;
        Breadcrumb((page.Title, null));
    }
}
