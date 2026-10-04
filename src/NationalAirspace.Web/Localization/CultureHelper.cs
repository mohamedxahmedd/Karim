using System.Globalization;
using NationalAirspace.Application.Common;

namespace NationalAirspace.Web.Localization;

/// <summary>Maps URL language codes (ar/en) to formatting cultures (ar-EG / en-US).</summary>
public static class CultureHelper
{
    public const string ArabicCulture = "ar-EG";
    public const string EnglishCulture = "en-US";
    public const string RouteKey = "culture";

    public static readonly CultureInfo[] SupportedCultures = [Create(ArabicCulture), Create(EnglishCulture)];

    public static Lang CurrentLang => LangExtensions.FromCode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

    public static string CurrentCode => CurrentLang.Code();

    public static string CultureNameFor(string? code) =>
        LangExtensions.FromCode(code) == Lang.Ar ? ArabicCulture : EnglishCulture;

    public static CultureInfo Create(string name)
    {
        var culture = new CultureInfo(name);
        // Always Gregorian dates with Latin digits for consistent, readable official content.
        if (culture.TwoLetterISOLanguageName == "ar")
        {
            var gregorian = culture.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault();
            if (gregorian is not null) culture.DateTimeFormat.Calendar = gregorian;
        }
        return culture;
    }

    /// <summary>Replaces (or adds) the culture segment of a site path, e.g. /ar/news → /en/news.</summary>
    public static string SwitchPath(string path, string targetCode)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (segments.Count > 0 && LangExtensions.IsSupported(segments[0])) segments[0] = targetCode;
        else segments.Insert(0, targetCode);
        return "/" + string.Join('/', segments);
    }
}
