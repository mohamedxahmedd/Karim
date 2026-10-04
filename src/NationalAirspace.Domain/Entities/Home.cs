using NationalAirspace.Domain.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Domain.Entities;

public class HeroSlide : AuditableEntity, IOrderable
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? SubtitleAr { get; set; }
    public string? SubtitleEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public Guid? ImageId { get; set; }
    public MediaFile? Image { get; set; }
    public string? PrimaryButtonTextAr { get; set; }
    public string? PrimaryButtonTextEn { get; set; }
    public string? PrimaryButtonUrl { get; set; }
    public string? SecondaryButtonTextAr { get; set; }
    public string? SecondaryButtonTextEn { get; set; }
    public string? SecondaryButtonUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Configurable homepage block. Each <see cref="HomeSectionKey"/> exists once.</summary>
public class HomeSection : AuditableEntity, IOrderable
{
    public HomeSectionKey Key { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? SubtitleAr { get; set; }
    public string? SubtitleEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public Guid? ImageId { get; set; }
    public MediaFile? Image { get; set; }
    public string? ButtonTextAr { get; set; }
    public string? ButtonTextEn { get; set; }
    public string? ButtonUrl { get; set; }
    /// <summary>For list sections (news, reports …): how many items to show.</summary>
    public int ItemCount { get; set; } = 3;
    public bool IsVisible { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public class Statistic : AuditableEntity, IOrderable
{
    public string LabelAr { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ImportantLink : AuditableEntity, IOrderable
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MenuItem : AuditableEntity, IOrderable
{
    public MenuLocation Location { get; set; }
    public Guid? ParentId { get; set; }
    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    /// <summary>Site-relative path without culture (e.g. <c>/about</c>) or absolute https URL.</summary>
    public string Url { get; set; } = string.Empty;
    public bool OpenInNewTab { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;

    public bool IsExternal => Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                           || Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
