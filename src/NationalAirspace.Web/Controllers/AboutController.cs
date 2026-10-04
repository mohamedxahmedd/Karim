using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Localization;
using NationalAirspace.Application.Features.Departments;
using NationalAirspace.Application.Features.Leadership;
using NationalAirspace.Application.Features.Legislation;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Application.Features.Pages;
using NationalAirspace.Domain.Enums;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Localization;
using NationalAirspace.Web.ViewModels;

namespace NationalAirspace.Web.Controllers;

[Route("{culture:culture}/about")]
public sealed class AboutController(PagePublicService pages, IStringLocalizer<SharedResource> L) : PublicController
{
    private (string, string?) AboutCrumb => (L["Nav.About"], Url.Site("/about"));

    [HttpGet("")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public Task<IActionResult> Index(CancellationToken ct) => SystemPage(SystemPages.About, ct, isRoot: true);

    [HttpGet("vision-mission")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public Task<IActionResult> VisionMission(CancellationToken ct) => SystemPage(SystemPages.VisionMission, ct);

    [HttpGet("responsibilities")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public Task<IActionResult> Responsibilities(CancellationToken ct) => SystemPage(SystemPages.Responsibilities, ct);

    [HttpGet("structure")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Structure([FromServices] DepartmentPublicService departments, CancellationToken ct)
    {
        Seo(L["Nav.Structure"], L["Structure.Lead"]);
        ViewData["Lead"] = L["Structure.Lead"].Value;
        Breadcrumb(AboutCrumb, (L["Nav.Structure"], null));
        return View(await departments.GetTreeAsync(Lang, ct));
    }

    [HttpGet("leadership")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Leadership([FromServices] LeaderPublicService leaders, CancellationToken ct)
    {
        Seo(L["Nav.Leadership"]);
        Breadcrumb(AboutCrumb, (L["Nav.Leadership"], null));
        return View(await leaders.GetActiveAsync(Lang, ct));
    }

    [HttpGet("legislation")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Legislation([FromServices] LegislationPublicService legislation, string? q, LegalDocumentCategory? category, int page = 1, CancellationToken ct = default)
    {
        Seo(L["Nav.Legislation"], L["Legislation.Lead"]);
        ViewData["Lead"] = L["Legislation.Lead"].Value;
        Breadcrumb(AboutCrumb, (L["Nav.Legislation"], null));
        var result = await legislation.SearchAsync(Lang, q, category, page, ct: ct);
        return View(new ListPageModel<Application.Features.Legislation.LegalDocumentDto>(result, q) { Category = category?.ToString() });
    }

    [HttpGet("legislation/{id:guid}/download")]
    public async Task<IActionResult> DownloadLegislation(Guid id, [FromServices] LegislationPublicService legislation, [FromServices] MediaService media, CancellationToken ct) =>
        await DownloadHelper.FileAsync(this, media, await legislation.GetDocumentFileIdAsync(id, ct), ct);

    private async Task<IActionResult> SystemPage(string key, CancellationToken ct, bool isRoot = false)
    {
        var page = await pages.GetBySystemKeyAsync(key, Lang, ct);
        if (page is null) return NotFound();
        Seo(page.Seo.Title ?? page.Title, page.Seo.Description, page.HeroImage?.Url);
        ViewData["Lead"] = page.Summary;
        Breadcrumb(isRoot ? [(L["Nav.About"], null)] : [AboutCrumb, (page.Title, null)]);
        return View("~/Views/Pages/Page.cshtml", page);
    }
}
