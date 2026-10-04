using System.ComponentModel.DataAnnotations;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.News;
using NationalAirspace.Application.Features.Pages;
using NationalAirspace.Application.Features.Reports;
using NationalAirspace.Application.Features.Services;
using NationalAirspace.Application.Features.Training;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Home;

public class HeroSlideInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleEn { get; set; } = string.Empty;
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? SubtitleAr { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? SubtitleEn { get; set; }
    [StringLength(1000, ErrorMessage = V.MaxLength)] public string? DescriptionAr { get; set; }
    [StringLength(1000, ErrorMessage = V.MaxLength)] public string? DescriptionEn { get; set; }
    public Guid? ImageId { get; set; }
    [StringLength(100, ErrorMessage = V.MaxLength)] public string? PrimaryButtonTextAr { get; set; }
    [StringLength(100, ErrorMessage = V.MaxLength)] public string? PrimaryButtonTextEn { get; set; }
    [StringLength(500, ErrorMessage = V.MaxLength), SafeLink] public string? PrimaryButtonUrl { get; set; }
    [StringLength(100, ErrorMessage = V.MaxLength)] public string? SecondaryButtonTextAr { get; set; }
    [StringLength(100, ErrorMessage = V.MaxLength)] public string? SecondaryButtonTextEn { get; set; }
    [StringLength(500, ErrorMessage = V.MaxLength), SafeLink] public string? SecondaryButtonUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class HomeSectionInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    /// <summary>Read-only identifier of the block.</summary>
    public HomeSectionKey Key { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleEn { get; set; } = string.Empty;
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? SubtitleAr { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? SubtitleEn { get; set; }
    [StringLength(4000, ErrorMessage = V.MaxLength)] public string? DescriptionAr { get; set; }
    [StringLength(4000, ErrorMessage = V.MaxLength)] public string? DescriptionEn { get; set; }
    public Guid? ImageId { get; set; }
    [StringLength(100, ErrorMessage = V.MaxLength)] public string? ButtonTextAr { get; set; }
    [StringLength(100, ErrorMessage = V.MaxLength)] public string? ButtonTextEn { get; set; }
    [StringLength(500, ErrorMessage = V.MaxLength), SafeLink] public string? ButtonUrl { get; set; }
    [Range(1, 12, ErrorMessage = V.Range)] public int ItemCount { get; set; } = 3;
    public bool IsVisible { get; set; } = true;
}

public class StatisticInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string LabelAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string LabelEn { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(30, ErrorMessage = V.MaxLength)] public string Value { get; set; } = string.Empty;
    [StringLength(64, ErrorMessage = V.MaxLength), RegularExpression("^bi-[a-z0-9-]+$", ErrorMessage = "Val.Icon")] public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ImportantLinkInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string TitleEn { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(500, ErrorMessage = V.MaxLength), SafeLink] public string Url { get; set; } = string.Empty;
    [StringLength(64, ErrorMessage = V.MaxLength), RegularExpression("^bi-[a-z0-9-]+$", ErrorMessage = "Val.Icon")] public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record HeroSlideAdminItem(Guid Id, string TitleAr, string TitleEn, string? ImageUrl, bool IsActive, int DisplayOrder);
public sealed record HomeSectionAdminItem(Guid Id, HomeSectionKey Key, string TitleAr, string TitleEn, bool IsVisible, int DisplayOrder);
public sealed record StatisticAdminItem(Guid Id, string LabelAr, string LabelEn, string Value, string? Icon, bool IsActive, int DisplayOrder);
public sealed record ImportantLinkAdminItem(Guid Id, string TitleAr, string TitleEn, string Url, string? Icon, bool IsActive, int DisplayOrder);

// ---- public ----

public sealed record LinkDto(string Text, string Url);

public sealed record HeroSlideDto(string Title, string? Subtitle, string? Description, ImageRef? Image, LinkDto? Primary, LinkDto? Secondary);

public sealed record HomeBlockDto(HomeSectionKey Key, string Title, string? Subtitle, string? Description, ImageRef? Image, LinkDto? Button, int ItemCount);

public sealed record StatisticDto(string Label, string Value, string? Icon);

public sealed record ImportantLinkDto(string Title, string Url, string? Icon);

public sealed record HomePageDto(
    IReadOnlyList<HeroSlideDto> Slides,
    IReadOnlyList<HomeBlockDto> Blocks,
    IReadOnlyList<PageSectionDto> VisionMission,
    IReadOnlyList<PageSectionDto> Responsibilities,
    IReadOnlyList<PageSectionDto> AtfmSteps,
    IReadOnlyList<ServiceCardDto> Services,
    IReadOnlyList<NewsCardDto> News,
    IReadOnlyList<ReportCardDto> Reports,
    IReadOnlyList<TrainingCardDto> Training,
    IReadOnlyList<StatisticDto> Statistics,
    IReadOnlyList<ImportantLinkDto> Links);
