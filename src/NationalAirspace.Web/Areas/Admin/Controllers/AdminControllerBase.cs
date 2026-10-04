using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Domain.Enums;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Localization;

namespace NationalAirspace.Web.Areas.Admin.Controllers;

/// <summary>Base class for the admin area: authenticated by default, flash messages, upload handling.</summary>
[Area("Admin")]
[Authorize]
public abstract class AdminControllerBase : Controller
{
    public const string FlashSuccess = "Flash.Success";
    public const string FlashError = "Flash.Error";

    private IStringLocalizer? _localizer;
    protected IStringLocalizer L => _localizer ??= HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();

    protected CancellationToken Ct => HttpContext.RequestAborted;

    protected void Success(string key = "Admin.Saved") => TempData[FlashSuccess] = L[key].Value;

    protected void Failure(Result result) =>
        TempData[FlashError] = string.Join(" ", result.Errors.Select(e => L.Error(e)));

    /// <summary>Flashes the outcome of a command and returns to <paramref name="returnUrl"/> (if local) or Index.</summary>
    protected IActionResult AfterCommand(Result result, string? returnUrl, string successKey = "Admin.Saved")
    {
        if (result.IsNotFound) return NotFound();
        if (result.Succeeded) Success(successKey);
        else Failure(result);
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index");
    }

    /// <summary>
    /// If a file was posted in <paramref name="formField"/>, stores it in the media library and returns the new id;
    /// otherwise returns <paramref name="current"/>. Errors are added to ModelState under <paramref name="modelKey"/>.
    /// </summary>
    protected async Task<Guid?> UploadAsync(string formField, MediaKind kind, Guid? current, string modelKey)
    {
        var file = Request.HasFormContentType ? Request.Form.Files.GetFile(formField) : null;
        if (file is null || file.Length == 0) return current;

        var media = HttpContext.RequestServices.GetRequiredService<MediaService>();
        await using var stream = file.OpenReadStream();
        var result = await media.UploadAsync(stream, file.FileName, file.Length, kind, HttpContext.RequestAborted);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(modelKey, L.Error(error));
            return current;
        }
        ModelState.Remove(modelKey); // the posted (old) value must not override the new id when re-rendering
        return result.Value!.Id;
    }

    /// <summary>
    /// Standard create/edit POST flow: validate, run <paramref name="save"/>, flash and redirect on success
    /// (to Edit when <paramref name="next"/> is "edit", otherwise Index); re-display the form with errors otherwise.
    /// </summary>
    protected async Task<IActionResult> SaveAsync<TModel>(TModel model, Func<Task<Result<Guid>>> save, Func<TModel, Task<IActionResult>> redisplay, string? next, string prefix = "Input")
    {
        if (ModelState.IsValid)
        {
            var result = await save();
            if (result.IsNotFound) return NotFound();
            if (result.Succeeded)
            {
                Success();
                return next == "edit" ? RedirectToAction("Edit", new { id = result.Value }) : RedirectToAction("Index");
            }
            ModelState.AddResultErrors(result, L, prefix);
        }
        return await redisplay(model);
    }

    /// <summary>Adapts an update command (no id) to the <see cref="SaveAsync{TModel}"/> flow.</summary>
    protected static async Task<Result<Guid>> WithId(Guid id, Task<Result> command)
    {
        var result = await command;
        return result.Succeeded ? Result.Success(id) : Result<Guid>.Failure(result.Errors);
    }

    protected static IReadOnlyList<SelectListItem> Options<T>(IEnumerable<T> items, Func<T, Guid> id, Func<T, string> text, Guid? selected) =>
        items.Select(i => new SelectListItem(text(i), id(i).ToString(), id(i) == selected)).ToList();

    /// <summary>Maximum number of files accepted by one multi-file upload.</summary>
    protected const int MaxFilesPerUpload = 20;

    /// <summary>
    /// Stores several posted files in the media library. When <paramref name="kind"/> is null the kind is
    /// inferred from each file's extension. Per-file failures are flashed; the ids of stored files are returned.
    /// </summary>
    protected async Task<IReadOnlyList<Guid>> UploadManyAsync(IEnumerable<IFormFile> files, MediaKind? kind)
    {
        var media = HttpContext.RequestServices.GetRequiredService<MediaService>();
        var ids = new List<Guid>();
        var errors = new List<string>();
        foreach (var file in files.Where(f => f.Length > 0).Take(MaxFilesPerUpload))
        {
            var fileKind = kind;
            if (fileKind is null)
            {
                if (!FileSignatures.TryGetType(Path.GetExtension(file.FileName), out var detected, out _))
                {
                    errors.Add($"{file.FileName}: {L["Media.UnsupportedType"]}");
                    continue;
                }
                fileKind = detected;
            }
            await using var stream = file.OpenReadStream();
            var result = await media.UploadAsync(stream, file.FileName, file.Length, fileKind.Value, Ct);
            if (result.Succeeded) ids.Add(result.Value!.Id);
            else errors.Add($"{file.FileName}: {string.Join(" ", result.Errors.Select(e => L.Error(e)))}");
        }
        if (errors.Count > 0) TempData[FlashError] = string.Join(" | ", errors);
        return ids;
    }
}
