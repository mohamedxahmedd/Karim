using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Users;

namespace NationalAirspace.Infrastructure.Identity;

public sealed class AccountService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IClock clock) : IAccountService
{
    public async Task<LoginOutcome> SignInAsync(string email, string password, bool rememberMe, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null) return LoginOutcome.InvalidCredentials;

        // Password is verified first so that account state is only revealed to the legitimate owner.
        var check = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (check.IsLockedOut) return LoginOutcome.LockedOut;
        if (!check.Succeeded) return LoginOutcome.InvalidCredentials;
        if (!user.IsActive) return LoginOutcome.Inactive;

        user.LastLoginAtUtc = clock.UtcNow;
        await userManager.UpdateAsync(user);
        await signInManager.SignInAsync(user, isPersistent: rememberMe);
        return LoginOutcome.Success;
    }

    public Task SignOutAsync() => signInManager.SignOutAsync();

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        if (errors.Count > 0) return Result.Failure(errors);
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.Failure(Error.NotFound);

        var result = await userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
        if (!result.Succeeded)
        {
            return Result.Failure(result.Errors.Select(e => e.Code == "PasswordMismatch"
                ? new Error("Account.CurrentPasswordWrong", nameof(ChangePasswordInput.CurrentPassword))
                : new Error("Identity." + e.Code, nameof(ChangePasswordInput.NewPassword), e.Description)));
        }
        await signInManager.RefreshSignInAsync(user);
        return Result.Success();
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return null;
        var roles = await userManager.GetRolesAsync(user);
        return new UserProfileDto(user.Id, user.Email ?? string.Empty, user.FullName, roles.FirstOrDefault(), user.LastLoginAtUtc);
    }

    public async Task<Result> UpdateProfileAsync(Guid userId, string fullName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return Result.Failure(new Error(V.Required, "FullName"));
        if (fullName.Trim().Length > 150) return Result.Failure(new Error(V.MaxLength, "FullName"));
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.Failure(Error.NotFound);
        user.FullName = fullName.Trim();
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return Result.Failure(result.ToErrors());
        await signInManager.RefreshSignInAsync(user);
        return Result.Success();
    }
}
