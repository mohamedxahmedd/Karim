using Microsoft.AspNetCore.Mvc;
using NationalAirspace.Application.Features.Media;

namespace NationalAirspace.Web.Controllers;

internal static class DownloadHelper
{
    /// <summary>Streams a stored document as an attachment (404 if missing).</summary>
    public static async Task<IActionResult> FileAsync(Controller controller, MediaService media, Guid? mediaId, CancellationToken ct)
    {
        if (mediaId is not { } id) return controller.NotFound();
        var file = await media.OpenAsync(id, ct);
        if (file is null) return controller.NotFound();
        return controller.File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);
    }
}
