using NationalAirspace.Domain.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Domain.Entities;

public class Announcement : ScheduledEntity
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
    public Guid? AttachmentId { get; set; }
    public MediaFile? Attachment { get; set; }
    /// <summary>After this instant the announcement is no longer active and is archived automatically.</summary>
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsImportant { get; set; }

    public bool IsExpired(DateTime nowUtc) => ExpiresAtUtc is { } e && e <= nowUtc;

    /// <summary>Active = published, publication date reached, not expired.</summary>
    public override bool IsPubliclyVisible(DateTime nowUtc) => base.IsPubliclyVisible(nowUtc) && !IsExpired(nowUtc);

    /// <summary>Archives the announcement if it is published and expired. Returns true when the state changed.</summary>
    public bool ArchiveIfExpired(DateTime nowUtc)
    {
        if (Status != ContentStatus.Published || !IsExpired(nowUtc)) return false;
        Archive();
        return true;
    }
}
