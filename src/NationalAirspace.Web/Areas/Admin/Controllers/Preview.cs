using Microsoft.AspNetCore.Mvc;

namespace NationalAirspace.Web.Areas.Admin.Controllers;

/// <summary>
/// Renders unpublished content with the public layout from the admin area. The language comes from the
/// admin culture cookie; the layout's language switcher (/lang/{code}?returnUrl=…) returns to the same preview.
/// </summary>
internal static class Preview
{
    public static void Prepare(Controller controller, string title)
    {
        var request = controller.HttpContext.Request;
        controller.ViewData["Title"] = title;
        controller.ViewData["IsPreview"] = true;
        controller.ViewData["NoIndex"] = true;
        controller.ViewData["AlternatePath"] = request.PathBase + request.Path;
    }
}
