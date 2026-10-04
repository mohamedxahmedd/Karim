using NationalAirspace.Domain.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Domain.Entities;

public class Leader : AuditableEntity, IOrderable
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string PositionAr { get; set; } = string.Empty;
    public string PositionEn { get; set; } = string.Empty;
    public string? BiographyAr { get; set; }
    public string? BiographyEn { get; set; }
    public Guid? PhotoId { get; set; }
    public MediaFile? Photo { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Node of the organizational structure (self-referencing hierarchy).</summary>
public class Department : PublishableEntity, IOrderable
{
    public Guid? ParentDepartmentId { get; set; }
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> Children { get; set; } = new List<Department>();
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Returns true if making <paramref name="newParentId"/> the parent of <paramref name="departmentId"/>
    /// would create a cycle, given the current child → parent map.
    /// </summary>
    public static bool WouldCreateCycle(Guid departmentId, Guid? newParentId, IReadOnlyDictionary<Guid, Guid?> parentMap)
    {
        var visited = new HashSet<Guid>();
        var current = newParentId;
        while (current is { } id)
        {
            if (id == departmentId) return true;
            if (!visited.Add(id)) return true; // pre-existing corruption: treat as cycle
            current = parentMap.TryGetValue(id, out var parent) ? parent : null;
        }
        return false;
    }
}

public class LegalDocument : PublishableEntity
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public LegalDocumentCategory Category { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime PublicationDate { get; set; }
    public Guid? DocumentFileId { get; set; }
    public MediaFile? DocumentFile { get; set; }
}
