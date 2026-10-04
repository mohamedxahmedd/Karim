using System.Text;
using System.Text.RegularExpressions;

namespace NationalAirspace.Application.Common;

/// <summary>Builds URL slugs for Arabic and English text (Unicode letters are preserved).</summary>
public static partial class SlugGenerator
{
    public const int MaxLength = 150;

    public static string Generate(string? text, int maxLength = MaxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var sb = new StringBuilder(text.Length);
        var lastWasDash = false;
        foreach (var raw in text.Trim().ToLowerInvariant())
        {
            var c = NormalizeArabic(raw);
            if (c is null) continue;
            if (char.IsLetterOrDigit(c.Value))
            {
                sb.Append(c.Value);
                lastWasDash = false;
            }
            else if ((char.IsWhiteSpace(c.Value) || c is '-' or '_' or '.' or '/') && !lastWasDash && sb.Length > 0)
            {
                sb.Append('-');
                lastWasDash = true;
            }
        }

        var slug = sb.ToString().Trim('-');
        if (slug.Length > maxLength) slug = slug[..maxLength].TrimEnd('-');
        return slug;
    }

    public static bool IsValid(string? slug) =>
        !string.IsNullOrEmpty(slug) && slug.Length <= MaxLength && slug == slug.ToLowerInvariant() && SlugPattern().IsMatch(slug);

    /// <summary>Appends -2, -3 … until <paramref name="exists"/> returns false.</summary>
    public static async Task<string> EnsureUniqueAsync(string slug, Func<string, Task<bool>> exists)
    {
        var candidate = slug;
        for (var i = 2; await exists(candidate); i++)
        {
            var suffix = "-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var stem = slug.Length + suffix.Length > MaxLength ? slug[..(MaxLength - suffix.Length)] : slug;
            candidate = stem + suffix;
        }
        return candidate;
    }

    private static char? NormalizeArabic(char c) => c switch
    {
        >= 'ً' and <= 'ٟ' or 'ٰ' or 'ـ' => null, // diacritics and tatweel
        'أ' or 'إ' or 'آ' => 'ا',
        _ => c,
    };

    [GeneratedRegex(@"^[\p{L}\p{Nd}]+(?:-[\p{L}\p{Nd}]+)*$")]
    private static partial Regex SlugPattern();
}
