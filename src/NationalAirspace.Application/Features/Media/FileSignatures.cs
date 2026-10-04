using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Media;

/// <summary>Allow-listed upload types, verified by extension AND leading "magic" bytes.</summary>
public static class FileSignatures
{
    private sealed record FileType(MediaKind Kind, string ContentType, byte?[][] Signatures);

    private static readonly byte?[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte?[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte?[] Gif = [0x47, 0x49, 0x46, 0x38];
    private static readonly byte?[] Webp = [0x52, 0x49, 0x46, 0x46, null, null, null, null, 0x57, 0x45, 0x42, 0x50];
    private static readonly byte?[] Pdf = [0x25, 0x50, 0x44, 0x46];
    private static readonly byte?[] Zip = [0x50, 0x4B, 0x03, 0x04];
    private static readonly byte?[] Ole = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private static readonly Dictionary<string, FileType> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = new(MediaKind.Image, "image/jpeg", [Jpeg]),
        [".jpeg"] = new(MediaKind.Image, "image/jpeg", [Jpeg]),
        [".png"] = new(MediaKind.Image, "image/png", [Png]),
        [".gif"] = new(MediaKind.Image, "image/gif", [Gif]),
        [".webp"] = new(MediaKind.Image, "image/webp", [Webp]),
        [".pdf"] = new(MediaKind.Document, "application/pdf", [Pdf]),
        [".docx"] = new(MediaKind.Document, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", [Zip]),
        [".xlsx"] = new(MediaKind.Document, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [Zip]),
        [".pptx"] = new(MediaKind.Document, "application/vnd.openxmlformats-officedocument.presentationml.presentation", [Zip]),
        [".doc"] = new(MediaKind.Document, "application/msword", [Ole]),
        [".xls"] = new(MediaKind.Document, "application/vnd.ms-excel", [Ole]),
        [".ppt"] = new(MediaKind.Document, "application/vnd.ms-powerpoint", [Ole]),
    };

    public const int HeaderLength = 12;

    public static IEnumerable<string> ExtensionsFor(MediaKind kind) =>
        Types.Where(t => t.Value.Kind == kind).Select(t => t.Key);

    public static string AcceptAttribute(MediaKind kind) => string.Join(',', ExtensionsFor(kind));

    public static bool TryGetType(string extension, out MediaKind kind, out string contentType)
    {
        if (Types.TryGetValue(extension, out var type))
        {
            (kind, contentType) = (type.Kind, type.ContentType);
            return true;
        }
        (kind, contentType) = (default, string.Empty);
        return false;
    }

    /// <summary>True when <paramref name="header"/> starts with a signature registered for the extension.</summary>
    public static bool Matches(string extension, ReadOnlySpan<byte> header)
    {
        if (!Types.TryGetValue(extension, out var type)) return false;
        foreach (var signature in type.Signatures)
        {
            if (header.Length < signature.Length) continue;
            var ok = true;
            for (var i = 0; i < signature.Length && ok; i++)
                ok = signature[i] is null || header[i] == signature[i];
            if (ok) return true;
        }
        return false;
    }
}
