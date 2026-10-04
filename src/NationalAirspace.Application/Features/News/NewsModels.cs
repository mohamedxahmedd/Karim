using System.ComponentModel.DataAnnotations;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.News;

public class NewsInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)]
    public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)]
    public string TitleEn { get; set; } = string.Empty;
    [StringLength(1000, ErrorMessage = V.MaxLength)]
    public string? SummaryAr { get; set; }
    [StringLength(1000, ErrorMessage = V.MaxLength)]
    public string? SummaryEn { get; set; }
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
    [Slug, StringLength(150, ErrorMessage = V.MaxLength)]
    public string? SlugAr { get; set; }
    [Slug, StringLength(150, ErrorMessage = V.MaxLength)]
    public string? SlugEn { get; set; }
    public Guid? FeaturedImageId { get; set; }
    public Guid? NewsCategoryId { get; set; }
    [StringLength(150, ErrorMessage = V.MaxLength)]
    public string? AuthorAr { get; set; }
    [StringLength(150, ErrorMessage = V.MaxLength)]
    public string? AuthorEn { get; set; }
    /// <summary>Site-local publication time; empty = "now" when published.</summary>
    public DateTime? PublishDateLocal { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public bool IsFeatured { get; set; }
    [StringLength(200, ErrorMessage = V.MaxLength)]
    public string? SeoTitleAr { get; set; }
    [StringLength(200, ErrorMessage = V.MaxLength)]
    public string? SeoTitleEn { get; set; }
    [StringLength(400, ErrorMessage = V.MaxLength)]
    public string? MetaDescriptionAr { get; set; }
    [StringLength(400, ErrorMessage = V.MaxLength)]
    public string? MetaDescriptionEn { get; set; }
}

public sealed record NewsAdminListItem(
    Guid Id, string TitleAr, string TitleEn, ContentStatus Status, bool IsScheduled, bool IsFeatured,
    DateTime? PublishDateUtc, string? CategoryAr, string? CategoryEn, DateTime UpdatedAtUtc);

public class NewsCategoryInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)]
    public string NameAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)]
    public string NameEn { get; set; } = string.Empty;
    [Slug, StringLength(150, ErrorMessage = V.MaxLength)]
    public string? Slug { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record CategoryAdminItem(Guid Id, string NameAr, string NameEn, string Slug, bool IsActive, int DisplayOrder, int ItemCount);

public sealed record CategoryOption(Guid Id, string NameAr, string NameEn);

// ---- public ----

public sealed record NewsCardDto(
    Guid Id, string Title, string? Summary, string Slug, ImageRef? Image, DateTime PublishDateUtc,
    string? CategoryName, string? CategorySlug, bool IsFeatured);

public sealed record NewsDetailDto(
    Guid Id, string Title, string? Summary, string? ContentHtml, SlugPair Slugs, ImageRef? Image,
    DateTime PublishDateUtc, string? Author, string? CategoryName, string? CategorySlug, SeoMeta Seo,
    IReadOnlyList<NewsCardDto> Related);

public sealed record NewsCategoryDto(string Name, string Slug, int Count);
