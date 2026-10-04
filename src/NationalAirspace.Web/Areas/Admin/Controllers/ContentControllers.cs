using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Common.Security;
using NationalAirspace.Application.Features.Announcements;
using NationalAirspace.Application.Features.Legislation;
using NationalAirspace.Application.Features.News;
using NationalAirspace.Application.Features.Reports;
using NationalAirspace.Application.Features.Services;
using NationalAirspace.Application.Features.Training;
using NationalAirspace.Domain.Enums;
using NationalAirspace.Web.Areas.Admin.ViewModels;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Localization;

namespace NationalAirspace.Web.Areas.Admin.Controllers;

[Authorize(Policy = Permissions.News)]
public sealed class NewsController(NewsAdminService news) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? search, ContentStatus? status, Guid? categoryId, int page = 1)
    {
        var items = await news.ListAsync(new AdminListQuery(search, status, categoryId, page), Ct);
        var categories = Options(await news.GetCategoryOptionsAsync(Ct), c => c.Id, c => Bilingual.Pick(c.NameAr, c.NameEn), categoryId);
        return View(new AdminListModel<NewsAdminListItem>(items, new ListFilterModel(search, status, Categories: categories)));
    }

    [HttpGet]
    public Task<IActionResult> Create() => Form(new AdminFormModel<NewsInput>());

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await news.GetForEditAsync(id, Ct) is { } input ? await Form(new AdminFormModel<NewsInput> { Input = input }) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Create(AdminFormModel<NewsInput> model, string? next)
    {
        model.Input.FeaturedImageId = await UploadAsync("FeaturedImageUpload", MediaKind.Image, model.Input.FeaturedImageId, "Input.FeaturedImageId");
        return await SaveAsync(model, () => news.CreateAsync(model.Input, Ct), Form, next);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, AdminFormModel<NewsInput> model, string? next)
    {
        model.Input.Id = id;
        model.Input.FeaturedImageId = await UploadAsync("FeaturedImageUpload", MediaKind.Image, model.Input.FeaturedImageId, "Input.FeaturedImageId");
        return await SaveAsync(model, () => WithId(id, news.UpdateAsync(model.Input, Ct)), Form, next);
    }

    [HttpPost]
    public async Task<IActionResult> Status(Guid id, ContentStatus status, string? returnUrl) =>
        AfterCommand(await news.ChangeStatusAsync(id, status, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Feature(Guid id, string? returnUrl) =>
        AfterCommand(await news.ToggleFeaturedAsync(id, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) =>
        AfterCommand(await news.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    /// <summary>Renders the article with the public layout regardless of its status.</summary>
    [HttpGet]
    public async Task<IActionResult> Preview(Guid id, [FromServices] NewsPublicService publicNews)
    {
        var item = await publicNews.GetForPreviewAsync(CultureHelper.CurrentLang, id, Ct);
        if (item is null) return NotFound();
        Admin.Controllers.Preview.Prepare(this, item.Title);
        return View("~/Views/News/Details.cshtml", item);
    }

    private async Task<IActionResult> Form(AdminFormModel<NewsInput> model)
    {
        model.IsNew = model.Input.Id is null;
        model.Options = Options(await news.GetCategoryOptionsAsync(Ct), c => c.Id, c => Bilingual.Pick(c.NameAr, c.NameEn), model.Input.NewsCategoryId);
        return View("Edit", model);
    }
}

[Authorize(Policy = Permissions.News)]
public sealed class NewsCategoriesController(NewsCategoryAdminService categories) : AdminControllerBase
{
    public async Task<IActionResult> Index() => View("~/Areas/Admin/Views/Categories/Index.cshtml", await categories.ListAsync(Ct));

    [HttpGet]
    public IActionResult Create() => Form(new AdminFormModel<NewsCategoryInput>());

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await categories.GetForEditAsync(id, Ct) is { } input ? Form(new AdminFormModel<NewsCategoryInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<NewsCategoryInput> model) =>
        SaveAsync(model, () => categories.SaveAsync(model.Input, Ct), m => Task.FromResult(Form(m)), null);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<NewsCategoryInput> model)
    {
        model.Input.Id = id;
        return SaveAsync(model, () => categories.SaveAsync(model.Input, Ct), m => Task.FromResult(Form(m)), null);
    }

    [HttpPost]
    public async Task<IActionResult> Move(Guid id, int direction, string? returnUrl) =>
        AfterCommand(await categories.MoveAsync(id, direction, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) =>
        AfterCommand(await categories.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    private IActionResult Form(AdminFormModel<NewsCategoryInput> model)
    {
        model.IsNew = model.Input.Id is null;
        return View("~/Areas/Admin/Views/Categories/Edit.cshtml", model);
    }
}

[Authorize(Policy = Permissions.Reports)]
public sealed class ReportCategoriesController(ReportCategoryAdminService categories) : AdminControllerBase
{
    public async Task<IActionResult> Index() => View("~/Areas/Admin/Views/Categories/Index.cshtml", await categories.ListAsync(Ct));

    [HttpGet]
    public IActionResult Create() => Form(new AdminFormModel<NewsCategoryInput>());

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await categories.GetForEditAsync(id, Ct) is { } input ? Form(new AdminFormModel<NewsCategoryInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<NewsCategoryInput> model) =>
        SaveAsync(model, () => categories.SaveAsync(model.Input, Ct), m => Task.FromResult(Form(m)), null);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<NewsCategoryInput> model)
    {
        model.Input.Id = id;
        return SaveAsync(model, () => categories.SaveAsync(model.Input, Ct), m => Task.FromResult(Form(m)), null);
    }

    [HttpPost]
    public async Task<IActionResult> Move(Guid id, int direction, string? returnUrl) =>
        AfterCommand(await categories.MoveAsync(id, direction, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) =>
        AfterCommand(await categories.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    private IActionResult Form(AdminFormModel<NewsCategoryInput> model)
    {
        model.IsNew = model.Input.Id is null;
        return View("~/Areas/Admin/Views/Categories/Edit.cshtml", model);
    }
}

[Authorize(Policy = Permissions.Announcements)]
public sealed class AnnouncementsController(AnnouncementAdminService announcements) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? search, ContentStatus? status, int page = 1) =>
        View(new AdminListModel<AnnouncementAdminListItem>(await announcements.ListAsync(new AdminListQuery(search, status, null, page), Ct), new ListFilterModel(search, status)));

    [HttpGet]
    public IActionResult Create() => Form(new AdminFormModel<AnnouncementInput>());

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await announcements.GetForEditAsync(id, Ct) is { } input ? Form(new AdminFormModel<AnnouncementInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<AnnouncementInput> model, string? next) => Save(model, next);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<AnnouncementInput> model, string? next)
    {
        model.Input.Id = id;
        return Save(model, next);
    }

    [HttpPost]
    public async Task<IActionResult> Status(Guid id, ContentStatus status, string? returnUrl) =>
        AfterCommand(await announcements.ChangeStatusAsync(id, status, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) =>
        AfterCommand(await announcements.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    private async Task<IActionResult> Save(AdminFormModel<AnnouncementInput> model, string? next)
    {
        model.Input.AttachmentId = await UploadAsync("AttachmentUpload", MediaKind.Document, model.Input.AttachmentId, "Input.AttachmentId");
        return await SaveAsync(model, () => announcements.SaveAsync(model.Input, Ct), m => Task.FromResult(Form(m)), next);
    }

    private IActionResult Form(AdminFormModel<AnnouncementInput> model)
    {
        model.IsNew = model.Input.Id is null;
        return View("Edit", model);
    }
}

[Authorize(Policy = Permissions.Services)]
public sealed class ServicesController(ServiceAdminService services) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? search, ContentStatus? status, int page = 1) =>
        View(new AdminListModel<ServiceAdminListItem>(await services.ListAsync(new AdminListQuery(search, status, null, page, 50), Ct), new ListFilterModel(search, status)));

    [HttpGet]
    public IActionResult Create() => Form(new AdminFormModel<ServiceInput>());

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await services.GetForEditAsync(id, Ct) is { } input ? Form(new AdminFormModel<ServiceInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<ServiceInput> model, string? next) => Save(model, next);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<ServiceInput> model, string? next)
    {
        model.Input.Id = id;
        return Save(model, next);
    }

    [HttpPost]
    public async Task<IActionResult> Status(Guid id, ContentStatus status, string? returnUrl) =>
        AfterCommand(await services.ChangeStatusAsync(id, status, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Move(Guid id, int direction, string? returnUrl) =>
        AfterCommand(await services.MoveAsync(id, direction, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) =>
        AfterCommand(await services.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    private async Task<IActionResult> Save(AdminFormModel<ServiceInput> model, string? next)
    {
        model.Input.CoverImageId = await UploadAsync("CoverImageUpload", MediaKind.Image, model.Input.CoverImageId, "Input.CoverImageId");
        return await SaveAsync(model, () => services.SaveAsync(model.Input, Ct), m => Task.FromResult(Form(m)), next);
    }

    private IActionResult Form(AdminFormModel<ServiceInput> model)
    {
        model.IsNew = model.Input.Id is null;
        return View("Edit", model);
    }
}

[Authorize(Policy = Permissions.Reports)]
public sealed class ReportsController(ReportAdminService reports) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? search, ContentStatus? status, Guid? categoryId, int page = 1)
    {
        var items = await reports.ListAsync(new AdminListQuery(search, status, categoryId, page), Ct);
        var categories = Options(await reports.GetCategoryOptionsAsync(Ct), c => c.Id, c => Bilingual.Pick(c.NameAr, c.NameEn), categoryId);
        return View(new AdminListModel<ReportAdminListItem>(items, new ListFilterModel(search, status, Categories: categories)));
    }

    [HttpGet]
    public Task<IActionResult> Create() => Form(new AdminFormModel<ReportInput>());

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await reports.GetForEditAsync(id, Ct) is { } input ? await Form(new AdminFormModel<ReportInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<ReportInput> model, string? next) => Save(model, next);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<ReportInput> model, string? next)
    {
        model.Input.Id = id;
        return Save(model, next);
    }

    [HttpPost]
    public async Task<IActionResult> Status(Guid id, ContentStatus status, string? returnUrl) =>
        AfterCommand(await reports.ChangeStatusAsync(id, status, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) =>
        AfterCommand(await reports.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    private async Task<IActionResult> Save(AdminFormModel<ReportInput> model, string? next)
    {
        model.Input.PdfFileId = await UploadAsync("PdfUpload", MediaKind.Document, model.Input.PdfFileId, "Input.PdfFileId");
        model.Input.CoverImageId = await UploadAsync("CoverImageUpload", MediaKind.Image, model.Input.CoverImageId, "Input.CoverImageId");
        return await SaveAsync(model, () => reports.SaveAsync(model.Input, Ct), Form, next);
    }

    private async Task<IActionResult> Form(AdminFormModel<ReportInput> model)
    {
        model.IsNew = model.Input.Id is null;
        model.Options = Options(await reports.GetCategoryOptionsAsync(Ct), c => c.Id, c => Bilingual.Pick(c.NameAr, c.NameEn), model.Input.ReportCategoryId);
        return View("Edit", model);
    }
}

[Authorize(Policy = Permissions.Training)]
public sealed class TrainingController(TrainingAdminService training) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? search, ContentStatus? status, int page = 1) =>
        View(new AdminListModel<TrainingAdminListItem>(await training.ListAsync(new AdminListQuery(search, status, null, page), Ct), new ListFilterModel(search, status)));

    [HttpGet]
    public IActionResult Create() => Form(new AdminFormModel<TrainingInput>());

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await training.GetForEditAsync(id, Ct) is { } input ? Form(new AdminFormModel<TrainingInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<TrainingInput> model, string? next) => Save(model, next);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<TrainingInput> model, string? next)
    {
        model.Input.Id = id;
        return Save(model, next);
    }

    [HttpPost]
    public async Task<IActionResult> Status(Guid id, ContentStatus status, string? returnUrl) =>
        AfterCommand(await training.ChangeStatusAsync(id, status, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) =>
        AfterCommand(await training.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    private async Task<IActionResult> Save(AdminFormModel<TrainingInput> model, string? next)
    {
        model.Input.CourseImageId = await UploadAsync("CourseImageUpload", MediaKind.Image, model.Input.CourseImageId, "Input.CourseImageId");
        model.Input.AttachmentId = await UploadAsync("AttachmentUpload", MediaKind.Document, model.Input.AttachmentId, "Input.AttachmentId");
        return await SaveAsync(model, () => training.SaveAsync(model.Input, Ct), m => Task.FromResult(Form(m)), next);
    }

    private IActionResult Form(AdminFormModel<TrainingInput> model)
    {
        model.IsNew = model.Input.Id is null;
        return View("Edit", model);
    }
}

[Authorize(Policy = Permissions.Legislation)]
public sealed class LegislationController(LegislationAdminService legislation) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? search, ContentStatus? status, LegalDocumentCategory? category, int page = 1)
    {
        ViewData["Category"] = category;
        return View(new AdminListModel<LegalDocumentAdminListItem>(
            await legislation.ListAsync(new AdminListQuery(search, status, null, page), category, Ct), new ListFilterModel(search, status)));
    }

    [HttpGet]
    public IActionResult Create() => Form(new AdminFormModel<LegalDocumentInput>());

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await legislation.GetForEditAsync(id, Ct) is { } input ? Form(new AdminFormModel<LegalDocumentInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<LegalDocumentInput> model, string? next) => Save(model, next);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<LegalDocumentInput> model, string? next)
    {
        model.Input.Id = id;
        return Save(model, next);
    }

    [HttpPost]
    public async Task<IActionResult> Status(Guid id, ContentStatus status, string? returnUrl) =>
        AfterCommand(await legislation.ChangeStatusAsync(id, status, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) =>
        AfterCommand(await legislation.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    private async Task<IActionResult> Save(AdminFormModel<LegalDocumentInput> model, string? next)
    {
        model.Input.DocumentFileId = await UploadAsync("DocumentUpload", MediaKind.Document, model.Input.DocumentFileId, "Input.DocumentFileId");
        return await SaveAsync(model, () => legislation.SaveAsync(model.Input, Ct), m => Task.FromResult(Form(m)), next);
    }

    private IActionResult Form(AdminFormModel<LegalDocumentInput> model)
    {
        model.IsNew = model.Input.Id is null;
        return View("Edit", model);
    }
}
