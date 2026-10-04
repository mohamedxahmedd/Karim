using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Common.Security;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Application.Features.MediaCenter;
using NationalAirspace.Domain.Enums;
using NationalAirspace.Web.Areas.Admin.ViewModels;
using NationalAirspace.Web.Extensions;

namespace NationalAirspace.Web.Areas.Admin.Controllers;

/// <summary>Media library: upload, browse, describe (alt text), delete unused files, pick from forms.</summary>
[Authorize(Policy = Permissions.Media)]
public sealed class MediaController(MediaService media) : AdminControllerBase
{
    private const long UploadRequestLimit = 200L * 1024 * 1024;

    public async Task<IActionResult> Index(string? search, MediaKind? kind, int page = 1)
    {
        ViewData["Kind"] = kind;
        ViewData["Search"] = search;
        return View(await media.ListAsync(search, kind, page, 24, Ct));
    }

    [HttpPost, RequestSizeLimit(UploadRequestLimit), RequestFormLimits(MultipartBodyLengthLimit = UploadRequestLimit)]
    public async Task<IActionResult> Upload(List<IFormFile> files)
    {
        var ids = await UploadManyAsync(files, kind: null);
        if (ids.Count > 0) TempData[FlashSuccess] = L["Media.Uploaded", ids.Count].Value;
        else if (TempData.Peek(FlashError) is null) TempData[FlashError] = L["Media.NoFilesSelected"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var file = await media.GetAsync(id, Ct);
        if (file is null) return NotFound();
        return View(new MediaEditModel(file, await media.IsInUseAsync(id, Ct))
        {
            Input = new MediaMetadataInput { AltTextAr = file.AltTextAr, AltTextEn = file.AltTextEn },
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, [Bind(Prefix = "Input")] MediaMetadataInput input)
    {
        if (ModelState.IsValid)
        {
            var result = await media.UpdateMetadataAsync(id, input, Ct);
            if (result.IsNotFound) return NotFound();
            if (result.Succeeded)
            {
                Success();
                return RedirectToAction(nameof(Edit), new { id });
            }
            ModelState.AddResultErrors(result, L);
        }
        var file = await media.GetAsync(id, Ct);
        return file is null ? NotFound() : View(new MediaEditModel(file, await media.IsInUseAsync(id, Ct)) { Input = input });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl)
    {
        var result = await media.DeleteAsync(id, Ct);
        // After a successful delete from the file's own page, go back to the library (that page no longer exists).
        var deletedOwnPage = result.Succeeded && returnUrl?.Contains($"/Edit/{id}", StringComparison.OrdinalIgnoreCase) == true;
        return AfterCommand(result, deletedOwnPage ? null : returnUrl, "Admin.Deleted");
    }

    /// <summary>Streams any stored file (including unpublished documents) to an authorized editor.</summary>
    [HttpGet]
    public async Task<IActionResult> Download(Guid id)
    {
        var file = await media.OpenAsync(id, Ct);
        return file is null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Library browser loaded into the media-picker modal.</summary>
    [HttpGet]
    public async Task<IActionResult> Picker(MediaKind kind = MediaKind.Image, string? search = null, int page = 1)
    {
        ViewData["Kind"] = kind;
        ViewData["Search"] = search;
        return PartialView("_Picker", await media.ListAsync(search, kind, page, 18, Ct));
    }
}

[Authorize(Policy = Permissions.Media)]
public sealed class AlbumsController(AlbumAdminService albums) : AdminControllerBase
{
    private const long UploadRequestLimit = 200L * 1024 * 1024;

    public async Task<IActionResult> Index(string? search, ContentStatus? status, int page = 1) =>
        View(new AdminListModel<AlbumAdminListItem>(await albums.ListAsync(new AdminListQuery(search, status, null, page), Ct), new ListFilterModel(search, status)));

    [HttpGet]
    public IActionResult Create() => Form(new AdminFormModel<AlbumInput>());

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await albums.GetForEditAsync(id, Ct) is { } input ? Form(new AdminFormModel<AlbumInput> { Input = input }) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create(AdminFormModel<AlbumInput> model)
    {
        if (!ModelState.IsValid) return Form(model);
        var result = await albums.SaveAsync(model.Input, Ct);
        if (!result.Succeeded)
        {
            ModelState.AddResultErrors(result, L);
            return Form(model);
        }
        Success("Albums.CreatedAddPhotos");
        return RedirectToAction(nameof(Photos), new { id = result.Value });
    }

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<AlbumInput> model, string? next)
    {
        model.Input.Id = id;
        return SaveAsync(model, () => albums.SaveAsync(model.Input, Ct), m => Task.FromResult(Form(m)), next);
    }

    [HttpPost]
    public async Task<IActionResult> Status(Guid id, ContentStatus status, string? returnUrl) =>
        AfterCommand(await albums.ChangeStatusAsync(id, status, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Move(Guid id, int direction, string? returnUrl) =>
        AfterCommand(await albums.MoveAsync(id, direction, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) =>
        AfterCommand(await albums.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    // ---- photos ----

    [HttpGet]
    public async Task<IActionResult> Photos(Guid id)
    {
        var album = await albums.GetForEditAsync(id, Ct);
        if (album is null) return NotFound();
        return View(new AlbumPhotosModel(album, await albums.GetPhotosAsync(id, Ct)));
    }

    [HttpPost, RequestSizeLimit(UploadRequestLimit), RequestFormLimits(MultipartBodyLengthLimit = UploadRequestLimit)]
    public async Task<IActionResult> UploadPhotos(Guid id, List<IFormFile> files)
    {
        var ids = await UploadManyAsync(files, MediaKind.Image);
        if (ids.Count == 0)
        {
            if (TempData.Peek(FlashError) is null) TempData[FlashError] = L["Media.NoFilesSelected"].Value;
            return RedirectToAction(nameof(Photos), new { id });
        }
        var result = await albums.AddPhotosAsync(id, ids, Ct);
        if (result.IsNotFound) return NotFound();
        if (result.Succeeded) TempData[FlashSuccess] = L["Media.Uploaded", ids.Count].Value;
        else Failure(result);
        return RedirectToAction(nameof(Photos), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Caption(Guid photoId, [Bind(Prefix = "Caption")] AlbumPhotoCaptionInput input) =>
        await PhotoCommand(photoId, () => albums.UpdatePhotoCaptionAsync(photoId, input, Ct));

    [HttpPost]
    public async Task<IActionResult> Cover(Guid photoId) => await PhotoCommand(photoId, () => albums.SetCoverAsync(photoId, Ct));

    [HttpPost]
    public async Task<IActionResult> MovePhoto(Guid photoId, int direction) => await PhotoCommand(photoId, () => albums.MovePhotoAsync(photoId, direction, Ct));

    [HttpPost]
    public async Task<IActionResult> RemovePhoto(Guid photoId) => await PhotoCommand(photoId, () => albums.RemovePhotoAsync(photoId, Ct), "Admin.Deleted");

    private async Task<IActionResult> PhotoCommand(Guid photoId, Func<Task<Result>> command, string successKey = "Admin.Saved")
    {
        var albumId = await albums.GetAlbumIdOfPhotoAsync(photoId, Ct);
        if (albumId is null) return NotFound();
        var result = await command();
        if (result.Succeeded) Success(successKey);
        else Failure(result);
        return RedirectToAction(nameof(Photos), new { id = albumId });
    }

    private IActionResult Form(AdminFormModel<AlbumInput> model)
    {
        model.IsNew = model.Input.Id is null;
        return View("Edit", model);
    }
}

[Authorize(Policy = Permissions.Media)]
public sealed class VideosController(VideoAdminService videos) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? search, ContentStatus? status, int page = 1) =>
        View(new AdminListModel<VideoAdminListItem>(await videos.ListAsync(new AdminListQuery(search, status, null, page), Ct), new ListFilterModel(search, status)));

    [HttpGet]
    public IActionResult Create() => Form(new AdminFormModel<VideoInput>());

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await videos.GetForEditAsync(id, Ct) is { } input ? Form(new AdminFormModel<VideoInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<VideoInput> model, string? next) => Save(model, next);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<VideoInput> model, string? next)
    {
        model.Input.Id = id;
        return Save(model, next);
    }

    [HttpPost]
    public async Task<IActionResult> Status(Guid id, ContentStatus status, string? returnUrl) =>
        AfterCommand(await videos.ChangeStatusAsync(id, status, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Move(Guid id, int direction, string? returnUrl) =>
        AfterCommand(await videos.MoveAsync(id, direction, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) =>
        AfterCommand(await videos.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    private async Task<IActionResult> Save(AdminFormModel<VideoInput> model, string? next)
    {
        model.Input.ThumbnailId = await UploadAsync("ThumbnailUpload", MediaKind.Image, model.Input.ThumbnailId, "Input.ThumbnailId");
        return await SaveAsync(model, () => videos.SaveAsync(model.Input, Ct), m => Task.FromResult(Form(m)), next);
    }

    private IActionResult Form(AdminFormModel<VideoInput> model)
    {
        model.IsNew = model.Input.Id is null;
        return View("Edit", model);
    }
}
