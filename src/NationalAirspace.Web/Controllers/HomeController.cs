using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using NationalAirspace.Application.Features.Home;
using NationalAirspace.Web.Extensions;

namespace NationalAirspace.Web.Controllers;

[Route("{culture:culture}")]
public sealed class HomeController(HomePublicService home) : PublicController
{
    [HttpGet("")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await home.GetAsync(Lang, ct));
}
