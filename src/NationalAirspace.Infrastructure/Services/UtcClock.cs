using Microsoft.Extensions.Configuration;
using NationalAirspace.Application.Abstractions;

namespace NationalAirspace.Infrastructure.Services;

public sealed class UtcClock(IConfiguration configuration) : IClock
{
    private readonly TimeZoneInfo _zone = ResolveZone(configuration["Site:TimeZone"]);

    public DateTime UtcNow => DateTime.UtcNow;

    public TimeZoneInfo SiteTimeZone => _zone;

    private static TimeZoneInfo ResolveZone(string? id)
    {
        foreach (var candidate in new[] { id, "Africa/Cairo", "Egypt Standard Time" })
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            if (TimeZoneInfo.TryFindSystemTimeZoneById(candidate, out var zone)) return zone;
        }
        return TimeZoneInfo.Utc;
    }
}
