using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Common.Security;
using NationalAirspace.Application.Features.Home;
using NationalAirspace.Application.Features.Menus;
using NationalAirspace.Application.Features.Pages;
using NationalAirspace.Application.Features.Settings;
using NationalAirspace.Domain.Enums;
using NationalAirspace.Web.Areas.Admin.ViewModels;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Localization;

namespace NationalAirspace.Web.Areas.Admin.Controllers;

[Authorize(Policy = Permissions.Pages)]
public sealed class PagesController(PageAdminService pages) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? search, ContentStatus? status, int page = 1) =>
        View(new AdminListModel<PageAdminListItem>(await pages.ListAsync(new AdminListQuery(search, status, null, page, 50), Ct), new ListFilterModel(search, status)));

    [HttpGet]
    public Task<IActionResult> Create() => Form(new AdminFormModel<PageInput>());

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await pages.GetForEditAsync(id, Ct) is { } input ? await Form(new AdminFormModel<PageInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<PageInput> model, string? next) => Save(model, next ?? "edit");

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, AdminFormModel<PageInput> model, string? next)
    {
        // The system key is fixed at seeding time and never taken from the form.
        var current = await pages.GetForEditAsync(id, Ct);
        if (current is null) return NotFound();
        model.Input.Id = id;
        model.Input.SystemKey = current.SystemKey;
        return await Save(model, next);
    }

    [HttpPost]
    public async Task<IActionResult> Status(Guid id, ContentStatus status, string? returnUrl) => AfterCommand(await pages.ChangeStatusAsync(id, status, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) => AfterCommand(await pages.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    [HttpGet]
    public async Task<IActionResult> Preview(Guid id, [FromServices] PagePublicService publicPages)
    {
        var page = await publicPages.GetForPreviewAsync(id, CultureHelper.CurrentLang, Ct);
        if (page is null) return NotFound();
        Admin.Controllers.Preview.Prepare(this, page.Title);
        ViewData["Lead"] = page.Summary;
        return View(page.SystemKey == SystemPages.Atfm ? "~/Views/Pages/Atfm.cshtml" : "~/Views/Pages/Page.cshtml", page);
    }

    // ---- sections ----

    [HttpGet]
    public async Task<IActionResult> CreateSection(Guid pageId) =>
        await pages.GetForEditAsync(pageId, Ct) is { } page
            ? SectionForm(new AdminFormModel<PageSectionInput> { Input = new PageSectionInput { PageId = pageId, IsVisible = true } }, page)
            : NotFound();

    [HttpGet]
    public async Task<IActionResult> EditSection(Guid id)
    {
        var section = await pages.GetSectionForEditAsync(id, Ct);
        var page = section is null ? null : await pages.GetForEditAsync(section.PageId, Ct);
        return section is null || page is null ? NotFound() : SectionForm(new AdminFormModel<PageSectionInput> { Input = section }, page);
    }

    [HttpPost]
    public Task<IActionResult> CreateSection(Guid pageId, AdminFormModel<PageSectionInput> model)
    {
        model.Input.Id = null;
        model.Input.PageId = pageId;
        return SaveSection(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditSection(Guid id, AdminFormModel<PageSectionInput> model)
    {
        var existing = await pages.GetSectionForEditAsync(id, Ct);
        if (existing is null) return NotFound();
        model.Input.Id = id;
        model.Input.PageId = existing.PageId;
        return await SaveSection(model);
    }

    [HttpPost]
    public async Task<IActionResult> MoveSection(Guid id, int direction, string? returnUrl) => AfterCommand(await pages.MoveSectionAsync(id, direction, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> ToggleSection(Guid id, string? returnUrl) => AfterCommand(await pages.ToggleSectionAsync(id, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> DeleteSection(Guid id, string? returnUrl) => AfterCommand(await pages.DeleteSectionAsync(id, Ct), returnUrl, "Admin.Deleted");

    private async Task<IActionResult> Save(AdminFormModel<PageInput> model, string? next)
    {
        model.Input.HeroImageId = await UploadAsync("HeroImageUpload", MediaKind.Image, model.Input.HeroImageId, "Input.HeroImageId");
        return await SaveAsync(model, () => pages.SaveAsync(model.Input, Ct), Form, next);
    }

    private async Task<IActionResult> SaveSection(AdminFormModel<PageSectionInput> model)
    {
        model.Input.ImageId = await UploadAsync("SectionImageUpload", MediaKind.Image, model.Input.ImageId, "Input.ImageId");
        if (ModelState.IsValid)
        {
            var result = await pages.SaveSectionAsync(model.Input, Ct);
            if (result.IsNotFound) return NotFound();
            if (result.Succeeded)
            {
                Success();
                return RedirectToAction(nameof(Edit), null, new { id = model.Input.PageId }, "sections");
            }
            ModelState.AddResultErrors(result, L);
        }
        var page = await pages.GetForEditAsync(model.Input.PageId, Ct);
        return page is null ? NotFound() : SectionForm(model, page);
    }

    private async Task<IActionResult> Form(AdminFormModel<PageInput> model)
    {
        model.IsNew = model.Input.Id is null;
        ViewData["Sections"] = model.Input.Id is { } id ? await pages.GetSectionsAsync(id, Ct) : Array.Empty<PageSectionAdminItem>();
        return View("Edit", model);
    }

    private IActionResult SectionForm(AdminFormModel<PageSectionInput> model, PageInput page)
    {
        model.IsNew = model.Input.Id is null;
        ViewData["PageTitle"] = Bilingual.Pick(page.TitleAr, page.TitleEn);
        return View("EditSection", model);
    }
}

[Authorize(Policy = Permissions.Homepage)]
public sealed class HeroSlidesController(HomeAdminService home) : AdminControllerBase
{
    public async Task<IActionResult> Index() => View(await home.GetSlidesAsync(Ct));

    [HttpGet]
    public IActionResult Create() => Form(new AdminFormModel<HeroSlideInput> { Input = new HeroSlideInput { IsActive = true } });

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await home.GetSlideAsync(id, Ct) is { } input ? Form(new AdminFormModel<HeroSlideInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<HeroSlideInput> model, string? next) => Save(model, next);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<HeroSlideInput> model, string? next)
    {
        model.Input.Id = id;
        return Save(model, next);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(Guid id, string? returnUrl) => AfterCommand(await home.ToggleSlideAsync(id, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Move(Guid id, int direction, string? returnUrl) => AfterCommand(await home.MoveSlideAsync(id, direction, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) => AfterCommand(await home.DeleteSlideAsync(id, Ct), returnUrl, "Admin.Deleted");

    private async Task<IActionResult> Save(AdminFormModel<HeroSlideInput> model, string? next)
    {
        model.Input.ImageId = await UploadAsync("SlideImageUpload", MediaKind.Image, model.Input.ImageId, "Input.ImageId");
        return await SaveAsync(model, () => home.SaveSlideAsync(model.Input, Ct), m => Task.FromResult(Form(m)), next);
    }

    private IActionResult Form(AdminFormModel<HeroSlideInput> model)
    {
        model.IsNew = model.Input.Id is null;
        return View("Edit", model);
    }
}

/// <summary>The homepage blocks are fixed (one per <see cref="HomeSectionKey"/>); editors change content, visibility and order.</summary>
[Authorize(Policy = Permissions.Homepage)]
public sealed class HomeSectionsController(HomeAdminService home) : AdminControllerBase
{
    public async Task<IActionResult> Index() => View(await home.GetSectionsAsync(Ct));

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await home.GetSectionAsync(id, Ct) is { } input ? Form(new AdminFormModel<HomeSectionInput> { Input = input }) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, AdminFormModel<HomeSectionInput> model)
    {
        var current = await home.GetSectionAsync(id, Ct);
        if (current is null) return NotFound();
        model.Input.Id = id;
        model.Input.Key = current.Key; // the block type is fixed
        model.Input.ImageId = await UploadAsync("SectionImageUpload", MediaKind.Image, model.Input.ImageId, "Input.ImageId");
        return await SaveAsync(model, () => WithId(id, home.SaveSectionAsync(model.Input, Ct)), m => Task.FromResult(Form(m)), null);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(Guid id, string? returnUrl) => AfterCommand(await home.ToggleSectionAsync(id, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Move(Guid id, int direction, string? returnUrl) => AfterCommand(await home.MoveSectionAsync(id, direction, Ct), returnUrl);

    private IActionResult Form(AdminFormModel<HomeSectionInput> model) => View("Edit", model);
}

[Authorize(Policy = Permissions.Homepage)]
public sealed class StatisticsController(HomeAdminService home) : AdminControllerBase
{
    public async Task<IActionResult> Index() => View(await home.GetStatisticsAsync(Ct));

    [HttpGet]
    public IActionResult Create() => Form(new AdminFormModel<StatisticInput> { Input = new StatisticInput { IsActive = true } });

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await home.GetStatisticAsync(id, Ct) is { } input ? Form(new AdminFormModel<StatisticInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<StatisticInput> model) =>
        SaveAsync(model, () => home.SaveStatisticAsync(model.Input, Ct), m => Task.FromResult(Form(m)), null);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<StatisticInput> model)
    {
        model.Input.Id = id;
        return SaveAsync(model, () => home.SaveStatisticAsync(model.Input, Ct), m => Task.FromResult(Form(m)), null);
    }

    [HttpPost]
    public async Task<IActionResult> Move(Guid id, int direction, string? returnUrl) => AfterCommand(await home.MoveStatisticAsync(id, direction, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) => AfterCommand(await home.DeleteStatisticAsync(id, Ct), returnUrl, "Admin.Deleted");

    private IActionResult Form(AdminFormModel<StatisticInput> model)
    {
        model.IsNew = model.Input.Id is null;
        return View("Edit", model);
    }
}

[Authorize(Policy = Permissions.Homepage)]
public sealed class ImportantLinksController(HomeAdminService home) : AdminControllerBase
{
    public async Task<IActionResult> Index() => View(await home.GetLinksAsync(Ct));

    [HttpGet]
    public IActionResult Create() => Form(new AdminFormModel<ImportantLinkInput> { Input = new ImportantLinkInput { IsActive = true } });

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await home.GetLinkAsync(id, Ct) is { } input ? Form(new AdminFormModel<ImportantLinkInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<ImportantLinkInput> model) =>
        SaveAsync(model, () => home.SaveLinkAsync(model.Input, Ct), m => Task.FromResult(Form(m)), null);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<ImportantLinkInput> model)
    {
        model.Input.Id = id;
        return SaveAsync(model, () => home.SaveLinkAsync(model.Input, Ct), m => Task.FromResult(Form(m)), null);
    }

    [HttpPost]
    public async Task<IActionResult> Move(Guid id, int direction, string? returnUrl) => AfterCommand(await home.MoveLinkAsync(id, direction, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) => AfterCommand(await home.DeleteLinkAsync(id, Ct), returnUrl, "Admin.Deleted");

    private IActionResult Form(AdminFormModel<ImportantLinkInput> model)
    {
        model.IsNew = model.Input.Id is null;
        return View("Edit", model);
    }
}

[Authorize(Policy = Permissions.Menus)]
public sealed class MenusController(MenuService menus) : AdminControllerBase
{
    public async Task<IActionResult> Index(MenuLocation location = MenuLocation.Header)
    {
        ViewData["Location"] = location;
        return View(await menus.GetTreeAsync(location, Ct));
    }

    [HttpGet]
    public Task<IActionResult> Create(MenuLocation location = MenuLocation.Header, Guid? parentId = null) =>
        Form(new AdminFormModel<MenuItemInput> { Input = new MenuItemInput { Location = location, ParentId = parentId, IsVisible = true } });

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await menus.GetForEditAsync(id, Ct) is { } input ? await Form(new AdminFormModel<MenuItemInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<MenuItemInput> model) => Save(model);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<MenuItemInput> model)
    {
        model.Input.Id = id;
        return Save(model);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(Guid id, string? returnUrl) => AfterCommand(await menus.ToggleVisibleAsync(id, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Move(Guid id, int direction, string? returnUrl) => AfterCommand(await menus.MoveAsync(id, direction, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) => AfterCommand(await menus.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    private async Task<IActionResult> Save(AdminFormModel<MenuItemInput> model)
    {
        if (ModelState.IsValid)
        {
            var result = await menus.SaveAsync(model.Input, Ct);
            if (result.IsNotFound) return NotFound();
            if (result.Succeeded)
            {
                Success();
                return RedirectToAction(nameof(Index), new { location = model.Input.Location });
            }
            ModelState.AddResultErrors(result, L);
        }
        return await Form(model);
    }

    private async Task<IActionResult> Form(AdminFormModel<MenuItemInput> model)
    {
        model.IsNew = model.Input.Id is null;
        var parents = await menus.GetParentOptionsAsync(model.Input.Location, model.Input.Id, Ct);
        model.Options = Options(parents, p => p.Id, p => Bilingual.Pick(p.TitleAr, p.TitleEn), model.Input.ParentId);
        return View("Edit", model);
    }
}

[Authorize(Policy = Permissions.Settings)]
public sealed class SettingsController(SettingsService settings) : AdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await settings.GetForEditAsync(Ct));

    [HttpPost]
    public async Task<IActionResult> Index(SettingsInput input)
    {
        if (ModelState.IsValid)
        {
            var result = await settings.UpdateAsync(input, Ct);
            if (result.Succeeded)
            {
                Success();
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddResultErrors(result, L, prefix: string.Empty);
        }
        return View(input);
    }
}
