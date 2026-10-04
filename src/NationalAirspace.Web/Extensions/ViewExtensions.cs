using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Web.Localization;

namespace NationalAirspace.Web.Extensions;

public static class UrlExtensions
{
    /// <summary>
    /// Builds a public URL in the current language: "/about" → "/ar/about", "/" → "/ar".
    /// Absolute http(s) URLs are returned unchanged.
    /// </summary>
    public static string Site(this IUrlHelper url, string? path) => SiteFor(CultureHelper.CurrentCode, path);

    /// <summary>"/news" + slug → "/ar/news/{escaped slug}".</summary>
    public static string Site(this IUrlHelper url, string basePath, string segment) =>
        SiteFor(CultureHelper.CurrentCode, $"{basePath.TrimEnd('/')}/{Uri.EscapeDataString(segment)}");

    public static string SiteFor(string code, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/") return "/" + code;
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return path;
        return "/" + code + (path.StartsWith('/') ? path : "/" + path);
    }

    public static bool IsExternal(string? url) =>
        url is not null && (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
}

public static class FormatExtensions
{
    /// <summary>UTC instant → site-local long date (e.g. "4 أكتوبر 2026").</summary>
    public static string LongDate(this IClock clock, DateTime utc) =>
        clock.ToSiteTime(utc).ToString("d MMMM yyyy", CultureInfo.CurrentCulture);

    public static string DateTimeText(this IClock clock, DateTime utc) =>
        clock.ToSiteTime(utc).ToString("d MMM yyyy - HH:mm", CultureInfo.CurrentCulture);

    /// <summary>Date-only values (stored as UTC midnight) are formatted without time-zone conversion.</summary>
    public static string DateOnlyText(this DateTime date) => date.ToString("d MMMM yyyy", CultureInfo.CurrentCulture);

    public static string IsoDate(this IClock clock, DateTime utc) => clock.ToSiteTime(utc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string FileSize(this long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1) { size /= 1024; unit++; }
        return $"{size.ToString(unit == 0 ? "0" : "0.#", CultureInfo.InvariantCulture)} {units[unit]}";
    }
}

public static class LocalizerExtensions
{
    public static string Enum<TEnum>(this IStringLocalizer localizer, TEnum value) where TEnum : struct, System.Enum =>
        localizer[$"Enum.{typeof(TEnum).Name}.{value}"].Value;

    public static IEnumerable<SelectListItem> EnumOptions<TEnum>(this IStringLocalizer localizer, TEnum? selected = null) where TEnum : struct, System.Enum =>
        System.Enum.GetValues<TEnum>().Select(v => new SelectListItem(localizer.Enum(v), v.ToString(), selected.HasValue && selected.Value.Equals(v)));

    /// <summary>Localizes an application error; Identity errors fall back to their English description.</summary>
    public static string Error(this IStringLocalizer localizer, Error error)
    {
        var text = localizer[error.MessageKey];
        if (text.ResourceNotFound)
            return error.Args.Length > 0 && error.Args[0] is string fallback ? fallback : error.MessageKey;
        try
        {
            return error.Args.Length > 0 ? string.Format(CultureInfo.CurrentCulture, text.Value, error.Args) : text.Value;
        }
        catch (FormatException)
        {
            return text.Value;
        }
    }
}

public static class ModelStateExtensions
{
    /// <summary>Copies failed-result errors into ModelState (field errors are keyed "{prefix}.{Field}").</summary>
    public static void AddResultErrors(this ModelStateDictionary modelState, Result result, IStringLocalizer localizer, string prefix = "Input")
    {
        foreach (var error in result.Errors)
        {
            var key = error.Field is null ? string.Empty : string.IsNullOrEmpty(prefix) ? error.Field : $"{prefix}.{error.Field}";
            var message = localizer.Error(error);
            // DataAnnotations-style keys expect the field display name as {0}.
            if (message.Contains("{0}", StringComparison.Ordinal) && error.Field is not null)
            {
                var name = localizer["Field." + error.Field];
                message = string.Format(CultureInfo.CurrentCulture, message, name.ResourceNotFound ? error.Field : name.Value, "", "");
            }
            modelState.AddModelError(key, message);
        }
    }
}

/// <summary>Picks the current UI language's value of a bilingual pair (admin lists show both columns' best match).</summary>
public static class Bilingual
{
    public static string Pick(string? ar, string? en) => CultureHelper.CurrentLang.Pick(ar, en);
}
