using System.ComponentModel.DataAnnotations;

namespace NationalAirspace.Application.Common;

/// <summary>Localization keys for validation messages (resolved by the web layer).</summary>
public static class V
{
    public const string Required = "Val.Required";
    public const string MaxLength = "Val.MaxLength";
    public const string Email = "Val.Email";
    public const string Phone = "Val.Phone";
    public const string Url = "Val.Url";
    public const string Range = "Val.Range";
    public const string Slug = "Val.Slug";
    public const string SlugTaken = "Val.SlugTaken";
    public const string Compare = "Val.Compare";
    public const string MinLength = "Val.MinLength";
}

public static class InputValidator
{
    /// <summary>Runs DataAnnotations validation and returns field-level errors.</summary>
    public static List<Error> Validate(object input)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);
        return results
            .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : [string.Empty])
                .Select(m => new Error(r.ErrorMessage ?? V.Required, string.IsNullOrEmpty(m) ? null : m)))
            .ToList();
    }

    /// <summary>True if the value is an absolute https/http URL or a site-relative path starting with "/".</summary>
    public static bool IsSafeLink(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        value = value.Trim();
        if (value.StartsWith('/')) return !value.StartsWith("//", StringComparison.Ordinal) && !value.Contains('\\');
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    public static bool IsHttpsUrl(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);
}

/// <summary>Accepts empty, site-relative ("/path") or absolute http(s) URLs; rejects javascript:, data: etc.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SafeLinkAttribute : ValidationAttribute
{
    public SafeLinkAttribute() : base(V.Url) { }
    public override bool IsValid(object? value) => InputValidator.IsSafeLink(value as string);
}

/// <summary>Accepts empty or absolute https URLs only.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class HttpsUrlAttribute : ValidationAttribute
{
    public HttpsUrlAttribute() : base(V.Url) { }
    public override bool IsValid(object? value) => InputValidator.IsHttpsUrl(value as string);
}

/// <summary>Accepts empty or a valid slug.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SlugAttribute : ValidationAttribute
{
    public SlugAttribute() : base(V.Slug) { }
    public override bool IsValid(object? value) => value is not string s || s.Length == 0 || SlugGenerator.IsValid(s);
}
