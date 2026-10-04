using NationalAirspace.Domain.Common;

namespace NationalAirspace.Domain.Entities;

/// <summary>A service offered by the center (named to avoid clashing with "service" classes).</summary>
public class CenterService : PublishableEntity, IOrderable, ISluggable
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? ShortDescriptionAr { get; set; }
    public string? ShortDescriptionEn { get; set; }
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
    /// <summary>Bootstrap Icons name, e.g. <c>bi-diagram-3</c>.</summary>
    public string? Icon { get; set; }
    public Guid? CoverImageId { get; set; }
    public MediaFile? CoverImage { get; set; }
    public int DisplayOrder { get; set; }
    public string SlugAr { get; set; } = string.Empty;
    public string SlugEn { get; set; } = string.Empty;
    public string? SeoTitleAr { get; set; }
    public string? SeoTitleEn { get; set; }
    public string? MetaDescriptionAr { get; set; }
    public string? MetaDescriptionEn { get; set; }
}
