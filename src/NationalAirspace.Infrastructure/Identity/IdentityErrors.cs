using Microsoft.AspNetCore.Identity;
using NationalAirspace.Application.Common;

namespace NationalAirspace.Infrastructure.Identity;

internal static class IdentityErrors
{
    /// <summary>Maps Identity errors to localizable errors ("Identity.{Code}"), keeping the description as fallback.</summary>
    public static Error[] ToErrors(this IdentityResult result, string? field = null) =>
        result.Errors.Select(e => new Error("Identity." + e.Code, FieldFor(e.Code) ?? field, e.Description)).ToArray();

    private static string? FieldFor(string code) => code switch
    {
        "DuplicateEmail" or "InvalidEmail" or "DuplicateUserName" or "InvalidUserName" => "Email",
        _ => null,
    };
}
