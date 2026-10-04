using Microsoft.AspNetCore.Mvc;
using NationalAirspace.Application.Features.Menus;
using NationalAirspace.Application.Features.Settings;
using NationalAirspace.Domain.Enums;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Localization;

namespace NationalAirspace.Web.ViewComponents;

public sealed record NavItem(string Title, string Href, bool External, bool NewTab, bool Active, IReadOnlyList<NavItem> Children);

/// <summary>Database-driven main navigation with active-item detection.</summary>
public sealed class HeaderMenuViewComponent(MenuService menus) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var nodes = await menus.GetMenuAsync(MenuLocation.Header, CultureHelper.CurrentLang);
        var path = HttpContext.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
        return View(nodes.Select(n => ToItem(n, path)).ToList());
    }

    internal static NavItem ToItem(MenuNode node, string currentPath)
    {
        var href = UrlExtensions.SiteFor(CultureHelper.CurrentCode, node.Url);
        var children = node.Children.Select(c => ToItem(c, currentPath)).ToList();
        var home = "/" + CultureHelper.CurrentCode;
        var active = !node.IsExternal && (href == home
            ? string.Equals(currentPath, home, StringComparison.OrdinalIgnoreCase)
            : currentPath.StartsWith(href, StringComparison.OrdinalIgnoreCase)
              && (currentPath.Length == href.Length || currentPath[href.Length] == '/'));
        return new NavItem(node.Title, href, node.IsExternal, node.OpenInNewTab, active || children.Any(c => c.Active), children);
    }
}

public sealed record FooterModel(SiteInfoDto Site, IReadOnlyList<NavItem> Links);

public sealed class FooterViewComponent(MenuService menus) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(SiteInfoDto site)
    {
        var nodes = await menus.GetMenuAsync(MenuLocation.Footer, CultureHelper.CurrentLang);
        return View(new FooterModel(site, nodes.Select(n => HeaderMenuViewComponent.ToItem(n, string.Empty)).ToList()));
    }
}
