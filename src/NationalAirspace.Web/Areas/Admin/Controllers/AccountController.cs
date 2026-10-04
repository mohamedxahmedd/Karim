using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Users;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Services;

namespace NationalAirspace.Web.Areas.Admin.Controllers;

public sealed class LoginModel
{
    [Required(ErrorMessage = V.Required), EmailAddress(ErrorMessage = V.Email), StringLength(256, ErrorMessage = V.MaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = V.Required), DataType(DataType.Password), StringLength(128, ErrorMessage = V.MaxLength)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public sealed class ProfileModel
{
    [Required(ErrorMessage = V.Required), StringLength(150, ErrorMessage = V.MaxLength)]
    public string FullName { get; set; } = string.Empty;
}

/// <summary>Sign-in, sign-out and self-service account management.</summary>
public sealed class AccountController(IAccountService accounts) : AdminControllerBase
{
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToLocal(returnUrl);
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginModel());
    }

    [HttpPost, AllowAnonymous, EnableRateLimiting(ServiceCollectionExtensions.LoginRateLimit)]
    public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var outcome = await accounts.SignInAsync(model.Email, model.Password, model.RememberMe, HttpContext.RequestAborted);
        if (outcome == LoginOutcome.Success) return RedirectToLocal(returnUrl);

        // Generic message for bad credentials (no user enumeration).
        ModelState.AddModelError(string.Empty, L[outcome switch
        {
            LoginOutcome.LockedOut => "Account.LockedOut",
            LoginOutcome.Inactive => "Account.Inactive",
            _ => "Account.InvalidLogin",
        }]);
        model.Password = string.Empty;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await accounts.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied() => View();

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var profile = await accounts.GetProfileAsync(User.Id()!.Value, HttpContext.RequestAborted);
        if (profile is null) return NotFound();
        ViewData["Profile"] = profile;
        return View(new ProfileModel { FullName = profile.FullName });
    }

    [HttpPost]
    public async Task<IActionResult> Profile(ProfileModel model)
    {
        var userId = User.Id()!.Value;
        if (ModelState.IsValid)
        {
            var result = await accounts.UpdateProfileAsync(userId, model.FullName, HttpContext.RequestAborted);
            if (result.Succeeded)
            {
                Success();
                return RedirectToAction(nameof(Profile));
            }
            ModelState.AddResultErrors(result, L, prefix: string.Empty);
        }
        ViewData["Profile"] = await accounts.GetProfileAsync(userId, HttpContext.RequestAborted);
        return View(model);
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordInput());

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordInput input)
    {
        if (!ModelState.IsValid) return View(input);
        var result = await accounts.ChangePasswordAsync(User.Id()!.Value, input, HttpContext.RequestAborted);
        if (result.Succeeded)
        {
            Success("Account.PasswordChanged");
            return RedirectToAction(nameof(Profile));
        }
        ModelState.AddResultErrors(result, L, prefix: string.Empty);
        return View(new ChangePasswordInput());
    }

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Dashboard");
}
