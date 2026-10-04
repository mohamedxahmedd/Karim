using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NationalAirspace.Application.Common.Security;
using NationalAirspace.Application.Features.Departments;
using NationalAirspace.Application.Features.Leadership;
using NationalAirspace.Domain.Enums;
using NationalAirspace.Web.Areas.Admin.ViewModels;
using NationalAirspace.Web.Extensions;

namespace NationalAirspace.Web.Areas.Admin.Controllers;

[Authorize(Policy = Permissions.Leadership)]
public sealed class LeadersController(LeaderAdminService leaders) : AdminControllerBase
{
    public async Task<IActionResult> Index() => View(await leaders.ListAsync(Ct));

    [HttpGet]
    public IActionResult Create() => Form(new AdminFormModel<LeaderInput>());

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await leaders.GetForEditAsync(id, Ct) is { } input ? Form(new AdminFormModel<LeaderInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<LeaderInput> model, string? next) => Save(model, next);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<LeaderInput> model, string? next)
    {
        model.Input.Id = id;
        return Save(model, next);
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(Guid id, string? returnUrl) => AfterCommand(await leaders.ToggleActiveAsync(id, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Move(Guid id, int direction, string? returnUrl) => AfterCommand(await leaders.MoveAsync(id, direction, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) => AfterCommand(await leaders.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    private async Task<IActionResult> Save(AdminFormModel<LeaderInput> model, string? next)
    {
        model.Input.PhotoId = await UploadAsync("PhotoUpload", MediaKind.Image, model.Input.PhotoId, "Input.PhotoId");
        return await SaveAsync(model, () => leaders.SaveAsync(model.Input, Ct), m => Task.FromResult(Form(m)), next);
    }

    private IActionResult Form(AdminFormModel<LeaderInput> model)
    {
        model.IsNew = model.Input.Id is null;
        return View("Edit", model);
    }
}

[Authorize(Policy = Permissions.Departments)]
public sealed class DepartmentsController(DepartmentAdminService departments) : AdminControllerBase
{
    public async Task<IActionResult> Index() => View(await departments.GetTreeAsync(Ct));

    [HttpGet]
    public Task<IActionResult> Create(Guid? parentId) => Form(new AdminFormModel<DepartmentInput> { Input = new DepartmentInput { ParentDepartmentId = parentId } });

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await departments.GetForEditAsync(id, Ct) is { } input ? await Form(new AdminFormModel<DepartmentInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<DepartmentInput> model) =>
        SaveAsync(model, () => departments.SaveAsync(model.Input, Ct), Form, null);

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<DepartmentInput> model)
    {
        model.Input.Id = id;
        return SaveAsync(model, () => departments.SaveAsync(model.Input, Ct), Form, null);
    }

    [HttpPost]
    public async Task<IActionResult> Status(Guid id, ContentStatus status, string? returnUrl) => AfterCommand(await departments.ChangeStatusAsync(id, status, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Move(Guid id, int direction, string? returnUrl) => AfterCommand(await departments.MoveAsync(id, direction, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) => AfterCommand(await departments.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    private async Task<IActionResult> Form(AdminFormModel<DepartmentInput> model)
    {
        model.IsNew = model.Input.Id is null;
        // A department cannot be moved under itself or its descendants (the service enforces this too).
        var parents = await departments.GetParentOptionsAsync(model.Input.Id, Ct);
        model.Options = Options(parents, p => p.Id, p => new string('—', p.Depth) + " " + Bilingual.Pick(p.NameAr, p.NameEn), model.Input.ParentDepartmentId);
        return View("Edit", model);
    }
}
