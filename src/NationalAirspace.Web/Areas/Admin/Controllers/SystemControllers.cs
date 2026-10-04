using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Common.Security;
using NationalAirspace.Application.Features.Audit;
using NationalAirspace.Application.Features.Contact;
using NationalAirspace.Application.Features.Users;
using NationalAirspace.Domain.Enums;
using NationalAirspace.Web.Areas.Admin.ViewModels;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Services;

namespace NationalAirspace.Web.Areas.Admin.Controllers;

/// <summary>Inbox for contact-form submissions.</summary>
[Authorize(Policy = Permissions.Messages)]
public sealed class MessagesController(ContactAdminService messages) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? search, ContactMessageStatus? status, int page = 1)
    {
        ViewData["Search"] = search;
        ViewData["Status"] = status;
        return View(await messages.ListAsync(search, status, page, 20, Ct));
    }

    /// <summary>Opening a new message marks it as read.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id) =>
        await messages.OpenAsync(id, Ct) is { } message ? View(message) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Status(Guid id, ContactMessageStatus status, string? returnUrl) =>
        AfterCommand(await messages.SetStatusAsync(id, status, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Notes(Guid id, string? notes) =>
        AfterCommand(await messages.SaveNotesAsync(id, notes, Ct), Url.Action(nameof(Details), new { id }));

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl)
    {
        var result = await messages.DeleteAsync(id, Ct);
        var fromDetails = returnUrl?.Contains($"/Details/{id}", StringComparison.OrdinalIgnoreCase) == true;
        return AfterCommand(result, result.Succeeded && fromDetails ? null : returnUrl, "Admin.Deleted");
    }
}

[Authorize(Policy = Permissions.Users)]
public sealed class UsersController(IUserAdminService users) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        ViewData["Search"] = search;
        return View(await users.ListAsync(search, page, 20, Ct));
    }

    [HttpGet]
    public IActionResult Create() => Form(new AdminFormModel<UserEditInput> { Input = new UserEditInput { Role = Roles.ContentEditor } });

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id) =>
        await users.GetForEditAsync(id, Ct) is { } input ? Form(new AdminFormModel<UserEditInput> { Input = input }) : NotFound();

    [HttpPost]
    public Task<IActionResult> Create(AdminFormModel<UserEditInput> model)
    {
        model.Input.Id = null;
        if (string.IsNullOrEmpty(model.Input.Password))
            ModelState.AddModelError("Input.Password", L["Users.PasswordRequired"]);
        return SaveAsync(model, () => users.CreateAsync(model.Input, Ct), m => Task.FromResult(Form(m)), null);
    }

    [HttpPost]
    public Task<IActionResult> Edit(Guid id, AdminFormModel<UserEditInput> model)
    {
        model.Input.Id = id;
        model.Input.Password = model.Input.ConfirmPassword = null; // passwords change only through Reset password
        ModelState.Remove("Input.Password");
        ModelState.Remove("Input.ConfirmPassword");
        return SaveAsync(model, () => WithId(id, users.UpdateAsync(model.Input, Ct)), m => Task.FromResult(Form(m)), null);
    }

    [HttpPost]
    public async Task<IActionResult> Activate(Guid id, bool active, string? returnUrl) =>
        AfterCommand(await users.SetActiveAsync(id, active, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Unlock(Guid id, string? returnUrl) =>
        AfterCommand(await users.UnlockAsync(id, Ct), returnUrl);

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl) =>
        AfterCommand(await users.DeleteAsync(id, Ct), returnUrl, "Admin.Deleted");

    [HttpGet]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var user = await users.GetForEditAsync(id, Ct);
        if (user is null) return NotFound();
        ViewData["UserEmail"] = user.Email;
        return View(new ResetPasswordInput());
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordInput input)
    {
        if (ModelState.IsValid)
        {
            var result = await users.ResetPasswordAsync(id, input, Ct);
            if (result.IsNotFound) return NotFound();
            if (result.Succeeded)
            {
                Success("Users.PasswordReset");
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddResultErrors(result, L, prefix: string.Empty);
        }
        ViewData["UserEmail"] = (await users.GetForEditAsync(id, Ct))?.Email;
        return View(new ResetPasswordInput());
    }

    private IActionResult Form(AdminFormModel<UserEditInput> model)
    {
        model.IsNew = model.Input.Id is null;
        model.Options = Roles.All.Select(r => new SelectListItem(L["Role." + r], r, r == model.Input.Role)).ToList();
        ViewData["IsSelf"] = model.Input.Id is { } id && id == User.Id();
        return View("Edit", model);
    }
}

[Authorize(Policy = Permissions.AuditLog)]
public sealed class AuditController(AuditQueryService audit) : AdminControllerBase
{
    public async Task<IActionResult> Index(string? search, string? entityType, int page = 1)
    {
        ViewData["Search"] = search;
        ViewData["EntityType"] = entityType;
        ViewData["EntityTypes"] = await audit.GetEntityTypesAsync(Ct);
        return View(await audit.ListAsync(search, entityType, page, 30, Ct));
    }
}
