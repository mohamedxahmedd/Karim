using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }

    /// <summary>The site's operating time zone (default Africa/Cairo); used to interpret admin-entered times.</summary>
    TimeZoneInfo SiteTimeZone { get; }
}

public static class ClockExtensions
{
    /// <summary>Converts a UTC instant to site-local time.</summary>
    public static DateTime ToSiteTime(this IClock clock, DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), clock.SiteTimeZone);

    /// <summary>Interprets a wall-clock value entered by an administrator as site-local time and converts it to UTC.</summary>
    public static DateTime FromSiteTime(this IClock clock, DateTime local)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (clock.SiteTimeZone.IsInvalidTime(unspecified)) unspecified = unspecified.AddHours(1); // DST gap
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, clock.SiteTimeZone);
    }

    public static DateTime? FromSiteTime(this IClock clock, DateTime? local) => local is { } l ? clock.FromSiteTime(l) : null;

    public static DateTime? ToSiteTime(this IClock clock, DateTime? utc) => utc is { } u ? clock.ToSiteTime(u) : null;

    /// <summary>Normalizes a date-only value to UTC midnight.</summary>
    public static DateTime AsUtcDate(this DateTime date) => DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
}

/// <summary>The authenticated administrator performing the current request (if any).</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string UserName { get; }
    string? IpAddress { get; }
}

/// <summary>Physical storage of uploaded files.</summary>
public interface IFileStorage
{
    /// <summary>Stores the content and returns the storage-relative path.</summary>
    Task<string> SaveAsync(Stream content, MediaKind kind, string extension, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, MediaKind kind, CancellationToken cancellationToken = default);

    /// <summary>Opens a stored file for reading, or returns null if it does not exist.</summary>
    Stream? OpenRead(string storagePath, MediaKind kind);
}

public interface IHtmlSanitizer
{
    /// <summary>Removes scripts, event handlers and disallowed markup from rich text.</summary>
    string? Sanitize(string? html);
}

/// <summary>Evicts cached public output after content changes.</summary>
public interface IContentCacheInvalidator
{
    Task InvalidateAsync(CancellationToken cancellationToken = default);
}
