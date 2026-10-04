using System.ComponentModel.DataAnnotations;
using NationalAirspace.Application.Common;

namespace NationalAirspace.Application.Features.Users;

public enum LoginOutcome
{
    Success,
    InvalidCredentials,
    LockedOut,
    Inactive,
}

public sealed record UserListItemDto(
    Guid Id,
    string Email,
    string FullName,
    string? Role,
    bool IsActive,
    bool IsLockedOut,
    DateTime? LastLoginAtUtc,
    DateTime CreatedAtUtc);

public sealed record UserProfileDto(Guid Id, string Email, string FullName, string? Role, DateTime? LastLoginAtUtc);

public class UserEditInput
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = V.Required), EmailAddress(ErrorMessage = V.Email), StringLength(256, ErrorMessage = V.MaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = V.Required), StringLength(150, ErrorMessage = V.MaxLength)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = V.Required)]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Required when creating a user; ignored on update.</summary>
    [DataType(DataType.Password), StringLength(128, ErrorMessage = V.MaxLength)]
    public string? Password { get; set; }

    [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = V.Compare)]
    public string? ConfirmPassword { get; set; }
}

public class ResetPasswordInput
{
    [Required(ErrorMessage = V.Required), DataType(DataType.Password), StringLength(128, ErrorMessage = V.MaxLength)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = V.Required), DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = V.Compare)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordInput
{
    [Required(ErrorMessage = V.Required), DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = V.Required), DataType(DataType.Password), StringLength(128, ErrorMessage = V.MaxLength)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = V.Required), DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = V.Compare)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public interface IAccountService
{
    Task<LoginOutcome> SignInAsync(string email, string password, bool rememberMe, CancellationToken ct = default);
    Task SignOutAsync();
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordInput input, CancellationToken ct = default);
    Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<Result> UpdateProfileAsync(Guid userId, string fullName, CancellationToken ct = default);
}

public interface IUserAdminService
{
    Task<PagedResult<UserListItemDto>> ListAsync(string? search, int page, int pageSize, CancellationToken ct = default);
    Task<UserEditInput?> GetForEditAsync(Guid id, CancellationToken ct = default);
    Task<Result<Guid>> CreateAsync(UserEditInput input, CancellationToken ct = default);
    Task<Result> UpdateAsync(UserEditInput input, CancellationToken ct = default);
    Task<Result> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task<Result> ResetPasswordAsync(Guid id, ResetPasswordInput input, CancellationToken ct = default);
    Task<Result> UnlockAsync(Guid id, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}
