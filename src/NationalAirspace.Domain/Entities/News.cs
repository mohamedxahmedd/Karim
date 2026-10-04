using NationalAirspace.Domain.Common;

namespace NationalAirspace.Domain.Entities;

public class NewsCategory : AuditableEntity, IOrderable
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<NewsArticle> Articles { get; set; } = new List<NewsArticle>();
}

public class NewsArticle : ScheduledEntity, ISluggable
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? SummaryAr { get; set; }
    public string? SummaryEn { get; set; }
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
    public string SlugAr { get; set; } = string.Empty;
    public string SlugEn { get; set; } = string.Empty;
    public Guid? FeaturedImageId { get; set; }
    public MediaFile? FeaturedImage { get; set; }
    public Guid? NewsCategoryId { get; set; }
    public NewsCategory? NewsCategory { get; set; }
    public string? AuthorAr { get; set; }
    public string? AuthorEn { get; set; }
    public bool IsFeatured { get; set; }
    public string? SeoTitleAr { get; set; }
    public string? SeoTitleEn { get; set; }
    public string? MetaDescriptionAr { get; set; }
    public string? MetaDescriptionEn { get; set; }
}
