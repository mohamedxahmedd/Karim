using NationalAirspace.Domain.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Domain.Entities;

/// <summary>A CMS page composed of an intro and ordered sections.</summary>
public class ContentPage : PublishableEntity, ISluggable
{
    /// <summary>Stable key for built-in pages (e.g. <c>about</c>, <c>atfm</c>); null for custom pages.</summary>
    public string? SystemKey { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string SlugAr { get; set; } = string.Empty;
    public string SlugEn { get; set; } = string.Empty;
    public string? SummaryAr { get; set; }
    public string? SummaryEn { get; set; }
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
    public Guid? HeroImageId { get; set; }
    public MediaFile? HeroImage { get; set; }
    public string? SeoTitleAr { get; set; }
    public string? SeoTitleEn { get; set; }
    public string? MetaDescriptionAr { get; set; }
    public string? MetaDescriptionEn { get; set; }
    public ICollection<PageSection> Sections { get; set; } = new List<PageSection>();

    public bool IsSystemPage => SystemKey is not null;
}

public class PageSection : AuditableEntity, IOrderable
{
    public Guid PageId { get; set; }
    public ContentPage? Page { get; set; }
    /// <summary>Optional stable key (e.g. <c>vision</c>, <c>mission</c>) used by the homepage.</summary>
    public string? Key { get; set; }
    public PageSectionLayout Layout { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
    public string? Icon { get; set; }
    public Guid? ImageId { get; set; }
    public MediaFile? Image { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}
