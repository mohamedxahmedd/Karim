using NationalAirspace.Domain.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Domain.Entities;

public class ContactMessage : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Organization { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ContactMessageStatus Status { get; set; } = ContactMessageStatus.New;
    public DateTime? ReadAtUtc { get; set; }
    public DateTime? RepliedAtUtc { get; set; }
    public string? AdminNotes { get; set; }
    /// <summary>SHA-256 hash of the sender IP (raw IPs are not stored).</summary>
    public string? IpHash { get; set; }
    public string? Culture { get; set; }

    public void MarkRead(DateTime nowUtc)
    {
        if (Status == ContactMessageStatus.New) Status = ContactMessageStatus.Read;
        ReadAtUtc ??= nowUtc;
    }

    public void MarkReplied(DateTime nowUtc)
    {
        Status = ContactMessageStatus.Replied;
        ReadAtUtc ??= nowUtc;
        RepliedAtUtc = nowUtc;
    }
}
