namespace NationalAirspace.Application.Common;

/// <summary>Public URLs of stored images (served as static files under /uploads).</summary>
public static class MediaUrls
{
    public const string ImagesRequestPath = "/uploads";

    public static string? Image(string? storagePath) =>
        string.IsNullOrEmpty(storagePath) ? null : $"{ImagesRequestPath}/{storagePath}";
}
