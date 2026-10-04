using System.Text.RegularExpressions;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Domain.ValueObjects;

/// <summary>A validated external video URL. Only absolute https URLs are accepted.</summary>
public sealed partial record VideoLink(string Url, VideoProvider Provider, string? ExternalId)
{
    public const int MaxLength = 500;

    public static bool TryCreate(string? input, out VideoLink? link)
    {
        link = null;
        if (string.IsNullOrWhiteSpace(input) || input.Length > MaxLength) return false;
        if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps) return false;
        if (string.IsNullOrEmpty(uri.Host)) return false;

        var host = uri.Host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal)) host = host[4..];
        if (host.StartsWith("m.", StringComparison.Ordinal)) host = host[2..];

        string? id = null;
        switch (host)
        {
            case "youtube.com" or "youtube-nocookie.com":
                if (uri.AbsolutePath == "/watch")
                    id = GetQueryValue(uri.Query, "v");
                else if (uri.AbsolutePath.StartsWith("/embed/", StringComparison.Ordinal)
                      || uri.AbsolutePath.StartsWith("/shorts/", StringComparison.Ordinal)
                      || uri.AbsolutePath.StartsWith("/live/", StringComparison.Ordinal))
                    id = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);
                if (id is null || !YouTubeId().IsMatch(id)) return false;
                link = new VideoLink(uri.ToString(), VideoProvider.YouTube, id);
                return true;
            case "youtu.be":
                id = uri.AbsolutePath.Trim('/');
                if (!YouTubeId().IsMatch(id)) return false;
                link = new VideoLink(uri.ToString(), VideoProvider.YouTube, id);
                return true;
            case "vimeo.com" or "player.vimeo.com":
                id = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                if (id is null || !VimeoId().IsMatch(id)) return false;
                link = new VideoLink(uri.ToString(), VideoProvider.Vimeo, id);
                return true;
            default:
                link = new VideoLink(uri.ToString(), VideoProvider.External, null);
                return true;
        }
    }

    /// <summary>Privacy-enhanced embed URL, or null when the provider cannot be embedded.</summary>
    public string? EmbedUrl => Provider switch
    {
        VideoProvider.YouTube => $"https://www.youtube-nocookie.com/embed/{ExternalId}",
        VideoProvider.Vimeo => $"https://player.vimeo.com/video/{ExternalId}",
        _ => null,
    };

    public string? ThumbnailUrl => Provider == VideoProvider.YouTube
        ? $"https://i.ytimg.com/vi/{ExternalId}/hqdefault.jpg"
        : null;

    private static string? GetQueryValue(string query, string key)
    {
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0] == key) return Uri.UnescapeDataString(kv[1]);
        }
        return null;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{11}$")]
    private static partial Regex YouTubeId();

    [GeneratedRegex("^[0-9]{1,12}$")]
    private static partial Regex VimeoId();
}
