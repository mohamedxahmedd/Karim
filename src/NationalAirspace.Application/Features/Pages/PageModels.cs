using System.ComponentModel.DataAnnotations;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Pages;

/// <summary>Keys of built-in pages that have dedicated public routes.</summary>
public static class SystemPages
{
    public const string About = "about";
    public const string VisionMission = "vision-mission";
    public const string Responsibilities = "responsibilities";
    public const string AirspaceManagement = "airspace-management";
    public const string Atfm = "atfm";

    public static readonly IReadOnlyList<string> All = [About, VisionMission, Responsibilities, AirspaceManagement, Atfm];
}

/// <summary>Keys of well-known sections referenced by the homepage.</summary>
public static class SectionKeys
{
    public const string Vision = "vision";
    public const string Mission = "mission";
}

public class PageInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleEn { get; set; } = string.Empty;
    [Slug, StringLength(150, ErrorMessage = V.MaxLength)] public string? SlugAr { get; set; }
    [Slug, StringLength(150, ErrorMessage = V.MaxLength)] public string? SlugEn { get; set; }
    [StringLength(1000, ErrorMessage = V.MaxLength)] public string? SummaryAr { get; set; }
    [StringLength(1000, ErrorMessage = V.MaxLength)] public string? SummaryEn { get; set; }
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
    public Guid? HeroImageId { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    [StringLength(200, ErrorMessage = V.MaxLength)] public string? SeoTitleAr { get; set; }
    [StringLength(200, ErrorMessage = V.MaxLength)] public string? SeoTitleEn { get; set; }
    [StringLength(400, ErrorMessage = V.MaxLength)] public string? MetaDescriptionAr { get; set; }
    [StringLength(400, ErrorMessage = V.MaxLength)] public string? MetaDescriptionEn { get; set; }
    /// <summary>Read-only: set for built-in pages.</summary>
    public string? SystemKey { get; set; }
}

public class PageSectionInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public Guid PageId { get; set; }
    [StringLength(100, ErrorMessage = V.MaxLength), RegularExpression("^[a-z0-9-]*$", ErrorMessage = V.Slug)] public string? Key { get; set; }
    public PageSectionLayout Layout { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleEn { get; set; } = string.Empty;
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
    [StringLength(64, ErrorMessage = V.MaxLength), IconName] public string? Icon { get; set; }
    public Guid? ImageId { get; set; }
    public bool IsVisible { get; set; } = true;
}

public sealed record PageAdminListItem(Guid Id, string TitleAr, string TitleEn, string? SystemKey, ContentStatus Status, int SectionCount, DateTime UpdatedAtUtc);

public sealed record PageSectionAdminItem(Guid Id, string? Key, PageSectionLayout Layout, string TitleAr, string TitleEn, bool IsVisible, int DisplayOrder);

// ---- public ----

public sealed record PageSectionDto(string? Key, PageSectionLayout Layout, string Title, string? ContentHtml, string? Icon, ImageRef? Image);

public sealed record PageDto(
    Guid Id, string? SystemKey, string Title, string? Summary, string? ContentHtml, ImageRef? HeroImage,
    SlugPair Slugs, SeoMeta Seo, IReadOnlyList<PageSectionDto> Sections, DateTime UpdatedAtUtc);
