using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace NationalAirspace.Application.Common;

/// <summary>
/// In-memory cache for rarely-changing public content (menus, settings, homepage blocks).
/// All entries share one change token so any content write clears them at once.
/// </summary>
public sealed class ContentCache(IMemoryCache cache)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private CancellationTokenSource _cts = new();

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory)
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null) return cached;
        var token = _cts.Token;
        var value = await factory();
        using var entry = cache.CreateEntry(key);
        entry.Value = value;
        entry.AbsoluteExpirationRelativeToNow = Lifetime;
        entry.AddExpirationToken(new CancellationChangeToken(token));
        return value;
    }

    public void Invalidate()
    {
        var old = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
        old.Cancel(); // not disposed: in-flight entries may still register on its token
    }
}
