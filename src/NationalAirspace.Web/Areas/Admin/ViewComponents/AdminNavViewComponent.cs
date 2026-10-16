using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NationalAirspace.Application.Common.Security;
using NationalAirspace.Application.Features.Contact;

namespace NationalAirspace.Web.Areas.Admin.ViewComponents;

public sealed record AdminNavLink(string Controller, string TextKey, string Icon, string Permission, int? Badge = null);
public sealed record AdminNavGroup(string TitleKey, IReadOnlyList<AdminNavLink> Links);

/// <summary>Sidebar navigation showing only the sections the current user may access (server checks still apply).</summary>
public sealed class AdminNavViewComponent(IAuthorizationService authorization, ContactAdminService messages) : ViewComponent
{
    private static readonly AdminNavGroup[] Groups =
    [
        new("AdminNav.Overview", [new("Dashboard", "AdminNav.Dashboard", "bi-speedometer2", Permissions.Dashboard)]),
        new("AdminNav.Website", [
            new("HeroSlides", "AdminNav.HeroSlides", "bi-easel", Permissions.Homepage),
            new("HomeSections", "AdminNav.HomeSections", "bi-layout-text-window", Permissions.Homepage),
            new("Statistics", "AdminNav.Statistics", "bi-123", Permissions.Homepage),
            new("ImportantLinks", "AdminNav.ImportantLinks", "bi-link-45deg", Permissions.Homepage),
            new("Pages", "AdminNav.Pages", "bi-file-earmark-richtext", Permissions.Pages),
            new("Menus", "AdminNav.Menus", "bi-menu-button-wide", Permissions.Menus),
            new("Footer", "AdminNav.Footer", "bi-layout-text-window-reverse", Permissions.Settings),
        ]),
        new("AdminNav.Content", [
            new("Services", "AdminNav.Services", "bi-grid", Permissions.Services),
            new("News", "AdminNav.News", "bi-newspaper", Permissions.News),
            new("NewsCategories", "AdminNav.NewsCategories", "bi-tags", Permissions.News),
            new("Announcements", "AdminNav.Announcements", "bi-megaphone", Permissions.Announcements),
            new("Reports", "AdminNav.Reports", "bi-file-earmark-text", Permissions.Reports),
            new("ReportCategories", "AdminNav.ReportCategories", "bi-bookmarks", Permissions.Reports),
            new("Training", "AdminNav.Training", "bi-mortarboard", Permissions.Training),
            new("Legislation", "AdminNav.Legislation", "bi-journal-text", Permissions.Legislation),
        ]),
        new("AdminNav.Organization", [
            new("Leaders", "AdminNav.Leaders", "bi-person-badge", Permissions.Leadership),
            new("Departments", "AdminNav.Departments", "bi-diagram-3", Permissions.Departments),
        ]),
        new("AdminNav.Media", [
            new("Media", "AdminNav.Library", "bi-folder2-open", Permissions.Media),
            new("Albums", "AdminNav.Albums", "bi-images", Permissions.Media),
            new("Videos", "AdminNav.Videos", "bi-camera-video", Permissions.Media),
        ]),
        new("AdminNav.Communication", [new("Messages", "AdminNav.Messages", "bi-envelope", Permissions.Messages)]),
        new("AdminNav.System", [
            new("Settings", "AdminNav.Settings", "bi-gear", Permissions.Settings),
            new("Users", "AdminNav.Users", "bi-people", Permissions.Users),
            new("Audit", "AdminNav.Audit", "bi-clock-history", Permissions.AuditLog),
        ]),
    ];

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var granted = new Dictionary<string, bool>();
        async Task<bool> Can(string permission)
        {
            if (!granted.TryGetValue(permission, out var ok))
                granted[permission] = ok = (await authorization.AuthorizeAsync(UserClaimsPrincipal, permission)).Succeeded;
            return ok;
        }

        var result = new List<AdminNavGroup>();
        foreach (var group in Groups)
        {
            var links = new List<AdminNavLink>();
            foreach (var link in group.Links)
            {
                if (!await Can(link.Permission)) continue;
                links.Add(link.Controller == "Messages" ? link with { Badge = await messages.CountNewAsync(HttpContext.RequestAborted) } : link);
            }
            if (links.Count > 0) result.Add(group with { Links = links });
        }
        ViewData["Current"] = RouteData.Values["controller"]?.ToString();
        return View(result);
    }
}
