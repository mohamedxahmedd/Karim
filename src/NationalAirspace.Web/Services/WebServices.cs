using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.OutputCaching;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Infrastructure.Identity;

namespace NationalAirspace.Web.Services;

/// <summary>Current administrator, taken from the authentication cookie.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string UserName => User?.FindFirstValue(ClaimTypes.Email) ?? User?.Identity?.Name ?? "system";

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}

/// <summary>Clears output-cached public pages and in-memory content after any CMS write.</summary>
public sealed class ContentCacheInvalidator(IOutputCacheStore store, ContentCache contentCache) : IContentCacheInvalidator
{
    public const string Tag = "content";

    public async Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        contentCache.Invalidate();
        await store.EvictByTagAsync(Tag, cancellationToken);
    }
}

/// <summary>Keyed hash of client IPs so contact submissions can be throttled without storing raw addresses.</summary>
public sealed class IpHasher(IConfiguration configuration)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(
        string.IsNullOrWhiteSpace(configuration["Security:IpHashKey"]) ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) : configuration["Security:IpHashKey"]!);

    public string? Hash(string? ip) =>
        string.IsNullOrEmpty(ip) ? null : Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(ip)));
}

/// <summary>Issues and reads a tamper-proof "form rendered at" token for bot mitigation.</summary>
public sealed class FormTimestampProtector(IDataProtectionProvider provider, IClock clock)
{
    private readonly IDataProtector _protector = provider.CreateProtector("NationalAirspace.ContactForm.Timestamp.v1");

    public string Issue() => _protector.Protect(clock.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public DateTime? Read(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;
        try
        {
            var ticks = long.Parse(_protector.Unprotect(token), System.Globalization.CultureInfo.InvariantCulture);
            return new DateTime(ticks, DateTimeKind.Utc);
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or FormatException or OverflowException)
        {
            return null;
        }
    }
}

public static class ClaimsPrincipalExtensions
{
    public static string DisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue(AppClaimsPrincipalFactory.FullNameClaim) ?? user.Identity?.Name ?? string.Empty;

    public static Guid? Id(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
