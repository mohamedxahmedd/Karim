using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Common.Security;
using NationalAirspace.Application.Features.Users;

namespace NationalAirspace.Infrastructure.Identity;

/// <summary>User &amp; role administration with safeguards against locking everybody out.</summary>
public sealed class UserAdminService(AdminServiceDependencies deps, UserManager<ApplicationUser> userManager)
    : AdminServiceBase(deps), IUserAdminService
{
    protected override string AuditEntityType => "User";

    public async Task<PagedResult<UserListItemDto>> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.Email!.Contains(term) || u.FullName.Contains(term));
        }
        var paged = await query.OrderBy(u => u.FullName).ToPagedAsync(page, pageSize, ct);
        var now = Clock.UtcNow;
        var items = new List<UserListItemDto>(paged.Items.Count);
        foreach (var u in paged.Items)
        {
            var roles = await userManager.GetRolesAsync(u);
            items.Add(new UserListItemDto(u.Id, u.Email ?? string.Empty, u.FullName, roles.FirstOrDefault(), u.IsActive,
                u.LockoutEnd is { } end && end.UtcDateTime > now, u.LastLoginAtUtc, u.CreatedAtUtc));
        }
        return new PagedResult<UserListItemDto>(items, paged.Page, paged.PageSize, paged.TotalCount);
    }

    public async Task<UserEditInput?> GetForEditAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return null;
        var roles = await userManager.GetRolesAsync(user);
        return new UserEditInput
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? string.Empty,
            IsActive = user.IsActive,
        };
    }

    public async Task<Result<Guid>> CreateAsync(UserEditInput input, CancellationToken ct = default)
    {
        var errors = Validate(input);
        if (string.IsNullOrEmpty(input.Password)) errors.Add(new Error(V.Required, nameof(input.Password)));
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        var user = new ApplicationUser
        {
            UserName = input.Email.Trim(),
            Email = input.Email.Trim(),
            EmailConfirmed = true,
            FullName = input.FullName.Trim(),
            IsActive = input.IsActive,
            CreatedAtUtc = Clock.UtcNow,
            LockoutEnabled = true,
        };
        var created = await userManager.CreateAsync(user, input.Password!);
        if (!created.Succeeded) return Result<Guid>.Failure(created.ToErrors(nameof(input.Password)));
        var roleResult = await userManager.AddToRoleAsync(user, input.Role);
        if (!roleResult.Succeeded) return Result<Guid>.Failure(roleResult.ToErrors());

        Audit("Create", user.Id, $"{user.Email} ({input.Role})");
        return await CommitAsync(user.Id, ct);
    }

    public async Task<Result> UpdateAsync(UserEditInput input, CancellationToken ct = default)
    {
        var errors = Validate(input);
        if (errors.Count > 0) return Result.Failure(errors);
        var user = input.Id is { } id ? await userManager.FindByIdAsync(id.ToString()) : null;
        if (user is null) return Result.Failure(Error.NotFound);

        var currentRoles = await userManager.GetRolesAsync(user);
        var guard = await GuardSuperAdminChangeAsync(user, currentRoles, input.Role, input.IsActive);
        if (guard is not null) return Result.Failure(guard);

        var securityRelevant = user.IsActive != input.IsActive || !currentRoles.SequenceEqual([input.Role])
                               || !string.Equals(user.Email, input.Email.Trim(), StringComparison.OrdinalIgnoreCase);
        user.Email = input.Email.Trim();
        user.UserName = input.Email.Trim();
        user.FullName = input.FullName.Trim();
        user.IsActive = input.IsActive;
        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded) return Result.Failure(updated.ToErrors());

        if (!currentRoles.SequenceEqual([input.Role]))
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            var added = await userManager.AddToRoleAsync(user, input.Role);
            if (!added.Succeeded) return Result.Failure(added.ToErrors());
        }
        // Invalidates existing cookies so role/activation changes apply immediately.
        if (securityRelevant) await userManager.UpdateSecurityStampAsync(user);

        Audit("Update", user.Id, $"{user.Email} ({input.Role}, active={input.IsActive})");
        return await CommitAsync(ct);
    }

    public async Task<Result> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result.Failure(Error.NotFound);
        var roles = await userManager.GetRolesAsync(user);
        var guard = await GuardSuperAdminChangeAsync(user, roles, roles.FirstOrDefault() ?? string.Empty, isActive);
        if (guard is not null) return Result.Failure(guard);

        user.IsActive = isActive;
        await userManager.UpdateAsync(user);
        await userManager.UpdateSecurityStampAsync(user);
        Audit(isActive ? "Activate" : "Deactivate", user.Id, user.Email);
        return await CommitAsync(ct);
    }

    public async Task<Result> ResetPasswordAsync(Guid id, ResetPasswordInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        if (errors.Count > 0) return Result.Failure(errors);
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result.Failure(Error.NotFound);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, input.NewPassword);
        if (!result.Succeeded) return Result.Failure(result.ToErrors(nameof(input.NewPassword)));
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        Audit("ResetPassword", user.Id, user.Email);
        return await CommitAsync(ct);
    }

    public async Task<Result> UnlockAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result.Failure(Error.NotFound);
        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.ResetAccessFailedCountAsync(user);
        Audit("Unlock", user.Id, user.Email);
        return await CommitAsync(ct);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result.Failure(Error.NotFound);
        if (user.Id == CurrentUser.UserId) return Result.Failure(new Error("Users.CannotModifySelf"));
        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(Roles.SuperAdmin) && await CountActiveSuperAdminsAsync() <= 1 && user.IsActive)
            return Result.Failure(new Error("Users.LastSuperAdmin"));

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded) return Result.Failure(result.ToErrors());
        Audit("Delete", id, user.Email);
        return await CommitAsync(ct);
    }

    private static List<Error> Validate(UserEditInput input)
    {
        var errors = InputValidator.Validate(input);
        if (!string.IsNullOrEmpty(input.Role) && !Roles.All.Contains(input.Role))
            errors.Add(new Error("Users.InvalidRole", nameof(input.Role)));
        return errors;
    }

    /// <summary>Prevents users from demoting/deactivating themselves and the last active SuperAdmin from disappearing.</summary>
    private async Task<Error?> GuardSuperAdminChangeAsync(ApplicationUser user, IList<string> currentRoles, string newRole, bool newActive)
    {
        var losesAccess = !newActive || newRole != currentRoles.FirstOrDefault();
        if (user.Id == CurrentUser.UserId && losesAccess) return new Error("Users.CannotModifySelf");

        var wasActiveSuperAdmin = currentRoles.Contains(Roles.SuperAdmin) && user.IsActive;
        var staysActiveSuperAdmin = newRole == Roles.SuperAdmin && newActive;
        if (wasActiveSuperAdmin && !staysActiveSuperAdmin && await CountActiveSuperAdminsAsync() <= 1)
            return new Error("Users.LastSuperAdmin");
        return null;
    }

    private async Task<int> CountActiveSuperAdminsAsync() =>
        (await userManager.GetUsersInRoleAsync(Roles.SuperAdmin)).Count(u => u.IsActive);
}
