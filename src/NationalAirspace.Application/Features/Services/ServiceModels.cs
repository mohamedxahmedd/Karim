using System.ComponentModel.DataAnnotations;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Services;

public class ServiceInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleEn { get; set; } = string.Empty;
    [StringLength(1000, ErrorMessage = V.MaxLength)] public string? ShortDescriptionAr { get; set; }
    [StringLength(1000, ErrorMessage = V.MaxLength)] public string? ShortDescriptionEn { get; set; }
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
    [StringLength(64, ErrorMessage = V.MaxLength), RegularExpression("^bi-[a-z0-9-]+$", ErrorMessage = "Val.Icon")] public string? Icon { get; set; }
    public Guid? CoverImageId { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    [Slug, StringLength(150, ErrorMessage = V.MaxLength)] public string? SlugAr { get; set; }
    [Slug, StringLength(150, ErrorMessage = V.MaxLength)] public string? SlugEn { get; set; }
    [StringLength(200, ErrorMessage = V.MaxLength)] public string? SeoTitleAr { get; set; }
    [StringLength(200, ErrorMessage = V.MaxLength)] public string? SeoTitleEn { get; set; }
    [StringLength(400, ErrorMessage = V.MaxLength)] public string? MetaDescriptionAr { get; set; }
    [StringLength(400, ErrorMessage = V.MaxLength)] public string? MetaDescriptionEn { get; set; }
}

public sealed record ServiceAdminListItem(Guid Id, string TitleAr, string TitleEn, string? Icon, ContentStatus Status, int DisplayOrder, DateTime UpdatedAtUtc);

public sealed record ServiceCardDto(Guid Id, string Title, string? ShortDescription, string? Icon, string Slug, ImageRef? Image);

public sealed record ServiceDetailDto(Guid Id, string Title, string? ShortDescription, string? ContentHtml, string? Icon,
    ImageRef? Image, SlugPair Slugs, SeoMeta Seo, IReadOnlyList<ServiceCardDto> Others);
