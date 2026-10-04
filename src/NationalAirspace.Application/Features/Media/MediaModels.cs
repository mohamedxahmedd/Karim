using System.ComponentModel.DataAnnotations;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Media;

public sealed class MediaOptions
{
    public const string SectionName = "Media";
    public long MaxImageBytes { get; set; } = 5 * 1024 * 1024;
    public long MaxDocumentBytes { get; set; } = 25 * 1024 * 1024;

    public long MaxBytesFor(MediaKind kind) => kind == MediaKind.Image ? MaxImageBytes : MaxDocumentBytes;
}

public sealed record MediaFileDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    MediaKind Kind,
    string? AltTextAr,
    string? AltTextEn,
    string? Url,
    DateTime CreatedAtUtc);

public sealed class MediaMetadataInput
{
    [StringLength(250, ErrorMessage = V.MaxLength)]
    public string? AltTextAr { get; set; }

    [StringLength(250, ErrorMessage = V.MaxLength)]
    public string? AltTextEn { get; set; }
}

/// <summary>A stored file opened for streaming to the client.</summary>
public sealed record MediaDownload(Stream Content, string ContentType, string FileName);
