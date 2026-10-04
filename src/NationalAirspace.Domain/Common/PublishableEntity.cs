using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Domain.Common;

/// <summary>Content with a Draft → Published → Archived workflow.</summary>
public abstract class PublishableEntity : AuditableEntity
{
    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    public virtual void Publish(DateTime nowUtc) => Status = ContentStatus.Published;

    public void Unpublish() => Status = ContentStatus.Draft;

    public void Archive() => Status = ContentStatus.Archived;

    /// <summary>Applies a workflow transition by status value.</summary>
    public void ChangeStatus(ContentStatus status, DateTime nowUtc)
    {
        switch (status)
        {
            case ContentStatus.Published: Publish(nowUtc); break;
            case ContentStatus.Draft: Unpublish(); break;
            case ContentStatus.Archived: Archive(); break;
            default: throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }
    }
}

/// <summary>Publishable content that has an explicit (possibly future) publication instant.</summary>
public abstract class ScheduledEntity : PublishableEntity
{
    /// <summary>When the content becomes publicly visible. A future value means "scheduled".</summary>
    public DateTime? PublishDateUtc { get; set; }

    public override void Publish(DateTime nowUtc)
    {
        base.Publish(nowUtc);
        PublishDateUtc ??= nowUtc;
    }

    public bool IsScheduled(DateTime nowUtc) => Status == ContentStatus.Published && PublishDateUtc > nowUtc;

    public virtual bool IsPubliclyVisible(DateTime nowUtc) =>
        Status == ContentStatus.Published && PublishDateUtc is { } d && d <= nowUtc;
}
