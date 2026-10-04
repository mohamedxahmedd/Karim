namespace NationalAirspace.Application.Common;

/// <summary>Supported content languages. Arabic is the default.</summary>
public enum Lang
{
    Ar = 0,
    En = 1,
}

public static class LangExtensions
{
    public const string ArabicCode = "ar";
    public const string EnglishCode = "en";
    public static readonly string[] SupportedCodes = [ArabicCode, EnglishCode];

    public static string Code(this Lang lang) => lang == Lang.Ar ? ArabicCode : EnglishCode;

    public static bool IsRtl(this Lang lang) => lang == Lang.Ar;

    public static Lang Other(this Lang lang) => lang == Lang.Ar ? Lang.En : Lang.Ar;

    public static Lang FromCode(string? code) =>
        string.Equals(code, EnglishCode, StringComparison.OrdinalIgnoreCase) ? Lang.En : Lang.Ar;

    public static bool IsSupported(string? code) =>
        code is not null && SupportedCodes.Contains(code, StringComparer.OrdinalIgnoreCase);

    /// <summary>Selects the value for the language, falling back to the other language when empty.</summary>
    public static string Pick(this Lang lang, string? ar, string? en)
    {
        var (primary, fallback) = lang == Lang.Ar ? (ar, en) : (en, ar);
        return !string.IsNullOrWhiteSpace(primary) ? primary : fallback ?? string.Empty;
    }

    /// <summary>Like <see cref="Pick"/>, but returns null when both values are empty.</summary>
    public static string? PickOrNull(this Lang lang, string? ar, string? en)
    {
        var value = lang.Pick(ar, en);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
