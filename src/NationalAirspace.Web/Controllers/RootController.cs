using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Search;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Localization;

namespace NationalAirspace.Web.Controllers;

/// <summary>Culture-less endpoints: root redirect, language switch, robots, sitemap and error pages.</summary>
public sealed class RootController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        var cookie = Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];
        var culture = cookie is null ? null : CookieRequestCultureProvider.ParseCookieValue(cookie)?.UICultures.FirstOrDefault().Value;
        var code = culture?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true ? LangExtensions.EnglishCode : LangExtensions.ArabicCode;
        return Redirect("/" + code);
    }

    /// <summary>Stores the preferred language (1 year) and returns to the equivalent page.</summary>
    [HttpGet("/lang/{code}")]
    public IActionResult SetLanguage(string code, string? returnUrl)
    {
        if (!LangExtensions.IsSupported(code)) return NotFound();
        var culture = CultureHelper.CultureNameFor(code);
        Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, HttpOnly = true, SameSite = SameSiteMode.Lax, Secure = Request.IsHttps });
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : Redirect("/" + code.ToLowerInvariant());
    }

    [HttpGet("/robots.txt")]
    [OutputCache(Duration = 3600)]
    public ContentResult Robots()
    {
        var host = $"{Request.Scheme}://{Request.Host}";
        return Content($"User-agent: *\nDisallow: /Admin/\nDisallow: /lang/\nAllow: /\n\nSitemap: {host}/sitemap.xml\n", "text/plain", Encoding.UTF8);
    }

    [HttpGet("/sitemap.xml")]
    [OutputCache(PolicyName = ServiceCollectionExtensions.PublicCachePolicy)]
    public async Task<IActionResult> Sitemap([FromServices] SiteSearchService search, CancellationToken ct)
    {
        var host = $"{Request.Scheme}://{Request.Host}";
        string[] staticPaths =
        [
            "", "/about", "/about/vision-mission", "/about/responsibilities", "/about/structure", "/about/leadership", "/about/legislation",
            "/services", "/airspace-management", "/airspace-demo", "/atfm", "/news", "/announcements", "/reports", "/training",
            "/media", "/media/photos", "/media/videos", "/contact",
        ];
        var basePaths = new Dictionary<SearchHitType, string>
        {
            [SearchHitType.News] = "/news", [SearchHitType.Service] = "/services", [SearchHitType.Report] = "/reports",
            [SearchHitType.Training] = "/training", [SearchHitType.Page] = "/pages",
        };

        var sb = new StringBuilder();
        using (var xml = XmlWriter.Create(sb, new XmlWriterSettings { Async = true, Indent = false }))
        {
            xml.WriteStartDocument();
            xml.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            foreach (var code in LangExtensions.SupportedCodes)
                foreach (var p in staticPaths)
                    WriteUrl(xml, $"{host}/{code}{p}", null);
            foreach (var entry in await search.GetSitemapEntriesAsync(ct))
                foreach (var lang in new[] { Lang.Ar, Lang.En })
                    WriteUrl(xml, $"{host}/{lang.Code()}{basePaths[entry.Type]}/{Uri.EscapeDataString(entry.Slugs.For(lang))}", entry.LastModifiedUtc);
            xml.WriteEndElement();
        }
        return Content(sb.ToString().Replace("utf-16", "utf-8", StringComparison.Ordinal), "application/xml", Encoding.UTF8);
    }

    private static void WriteUrl(XmlWriter xml, string loc, DateTime? lastModified)
    {
        xml.WriteStartElement("url");
        xml.WriteElementString("loc", loc);
        if (lastModified is { } d) xml.WriteElementString("lastmod", d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        xml.WriteEndElement();
    }

    /// <summary>Localized error page; the language is taken from the original (failed) URL.</summary>
    [Route("/error/{statusCode:int}")]
    public IActionResult Error(int statusCode)
    {
        var original = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath
                       ?? HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path;
        var segment = original?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (LangExtensions.IsSupported(segment))
        {
            var culture = CultureHelper.Create(CultureHelper.CultureNameFor(segment));
            System.Globalization.CultureInfo.CurrentCulture = culture;
            System.Globalization.CultureInfo.CurrentUICulture = culture;
        }
        var code = statusCode is 404 or 403 or 429 or 400 ? statusCode : 500;
        Response.StatusCode = statusCode;
        ViewData["NoIndex"] = true;
        return View("~/Views/Shared/Error.cshtml", code);
    }
}
