using NationalAirspace.Domain.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Domain.Entities;

/// <summary>An uploaded file in the media library (image or document).</summary>
public class MediaFile : AuditableEntity
{
    public string OriginalFileName { get; set; } = string.Empty;
    /// <summary>Storage-relative path, e.g. <c>images/2026/10/{guid}.jpg</c>.</summary>
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public MediaKind Kind { get; set; }
    public string? AltTextAr { get; set; }
    public string? AltTextEn { get; set; }
    public Guid? UploadedByUserId { get; set; }
}
