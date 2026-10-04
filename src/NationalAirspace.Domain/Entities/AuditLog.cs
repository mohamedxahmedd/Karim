using NationalAirspace.Domain.Common;

namespace NationalAirspace.Domain.Entities;

/// <summary>Append-only record of an administrative action.</summary>
public class AuditLog : Entity
{
    public DateTime CreatedAtUtc { get; set; }
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Summary { get; set; }
    public string? IpAddress { get; set; }
}
