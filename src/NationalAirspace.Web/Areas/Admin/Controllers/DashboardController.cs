using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NationalAirspace.Application.Common.Security;
using NationalAirspace.Application.Features.Dashboard;

namespace NationalAirspace.Web.Areas.Admin.Controllers;

[Authorize(Policy = Permissions.Dashboard)]
public sealed class DashboardController(DashboardService dashboard, IAuthorizationService authorization) : AdminControllerBase
{
    public async Task<IActionResult> Index()
    {
        // Message details and the audit trail are only loaded for users allowed to see them.
        var messages = (await authorization.AuthorizeAsync(User, Permissions.Messages)).Succeeded;
        var activity = (await authorization.AuthorizeAsync(User, Permissions.AuditLog)).Succeeded;
        ViewData["CanMessages"] = messages;
        ViewData["CanAudit"] = activity;
        return View(await dashboard.GetAsync(messages, activity, HttpContext.RequestAborted));
    }
}
