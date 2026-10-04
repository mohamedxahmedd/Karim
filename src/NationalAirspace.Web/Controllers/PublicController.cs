using Microsoft.AspNetCore.Mvc;
using NationalAirspace.Application.Common;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Localization;

namespace NationalAirspace.Web.Controllers;

/// <summary>Base class for public website controllers (culture-prefixed routes).</summary>
public abstract class PublicController : Controller
{
    protected static Lang Lang => CultureHelper.CurrentLang;

    protected void Seo(string title, string? description = null, string? imageUrl = null)
    {
        ViewData["Title"] = title;
        if (!string.IsNullOrWhiteSpace(description)) ViewData["MetaDescription"] = description;
        if (!string.IsNullOrWhiteSpace(imageUrl)) ViewData["OgImage"] = imageUrl;
    }

    /// <summary>Breadcrumb trail; the last item is the current page (no URL).</summary>
    protected void Breadcrumb(params (string Text, string? Url)[] items) => ViewData["Breadcrumbs"] = items;

    /// <summary>Sets the language-switcher target for pages whose URL differs per language (slugs).</summary>
    protected void AlternatePath(string basePath, SlugPair slugs)
    {
        var other = Lang.Other();
        ViewData["AlternatePath"] = UrlExtensions.SiteFor(other.Code(), $"{basePath}/{Uri.EscapeDataString(slugs.For(other))}");
    }

    /// <summary>Redirects to the canonical slug when the request used the other language's slug.</summary>
    protected IActionResult? RedirectToCanonical(string requestedSlug, SlugPair slugs, string basePath)
    {
        var canonical = slugs.For(Lang);
        return string.Equals(requestedSlug, canonical, StringComparison.Ordinal)
            ? null
            : RedirectPermanent(Url.Site(basePath, canonical));
    }
}
