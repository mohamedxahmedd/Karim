using System.ComponentModel.DataAnnotations;
using NationalAirspace.Application.Common;

namespace NationalAirspace.Application.Features.Settings;

public class SettingsInput
{
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string SiteNameAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string SiteNameEn { get; set; } = string.Empty;
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? TaglineAr { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? TaglineEn { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? AddressAr { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? AddressEn { get; set; }
    [StringLength(50, ErrorMessage = V.MaxLength), RegularExpression(@"^[0-9+()\-\s]{4,50}$", ErrorMessage = V.Phone)] public string? Phone { get; set; }
    [StringLength(50, ErrorMessage = V.MaxLength), RegularExpression(@"^[0-9+()\-\s]{4,50}$", ErrorMessage = V.Phone)] public string? Fax { get; set; }
    [StringLength(256, ErrorMessage = V.MaxLength), EmailAddress(ErrorMessage = V.Email)] public string? Email { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? WorkingHoursAr { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? WorkingHoursEn { get; set; }
    [StringLength(1000, ErrorMessage = V.MaxLength), HttpsUrl] public string? MapEmbedUrl { get; set; }
    [Range(-90, 90, ErrorMessage = V.Range)] public double? Latitude { get; set; }
    [Range(-180, 180, ErrorMessage = V.Range)] public double? Longitude { get; set; }
    [StringLength(500, ErrorMessage = V.MaxLength), HttpsUrl] public string? FacebookUrl { get; set; }
    [StringLength(500, ErrorMessage = V.MaxLength), HttpsUrl] public string? XUrl { get; set; }
    [StringLength(500, ErrorMessage = V.MaxLength), HttpsUrl] public string? LinkedInUrl { get; set; }
    [StringLength(500, ErrorMessage = V.MaxLength), HttpsUrl] public string? YouTubeUrl { get; set; }
    [StringLength(500, ErrorMessage = V.MaxLength), HttpsUrl] public string? InstagramUrl { get; set; }
    [StringLength(400, ErrorMessage = V.MaxLength)] public string? DefaultMetaDescriptionAr { get; set; }
    [StringLength(400, ErrorMessage = V.MaxLength)] public string? DefaultMetaDescriptionEn { get; set; }
}

/// <summary>Everything the public footer shows that is not contact info, social links or the footer menu.</summary>
public class FooterSettingsInput
{
    public Guid ConcurrencyStamp { get; set; }
    [StringLength(1000, ErrorMessage = V.MaxLength)] public string? FooterTextAr { get; set; }
    [StringLength(1000, ErrorMessage = V.MaxLength)] public string? FooterTextEn { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? FooterLinksTitleAr { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? FooterLinksTitleEn { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? FooterContactTitleAr { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? FooterContactTitleEn { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? FooterCopyrightAr { get; set; }
    [StringLength(250, ErrorMessage = V.MaxLength)] public string? FooterCopyrightEn { get; set; }
}

public sealed record SocialLink(string Network, string Url, string Icon);

/// <summary>Localized site information for layouts and the contact page.</summary>
public sealed record SiteInfoDto(
    string SiteName, string? Tagline, string? Address, string? Phone, string? Fax, string? Email,
    string? WorkingHours, string? MapEmbedUrl, double? Latitude, double? Longitude,
    IReadOnlyList<SocialLink> Social, FooterInfo Footer, string? DefaultMetaDescription);

/// <summary>Localized footer texts; null values mean "use the built-in default".</summary>
public sealed record FooterInfo(string? Text, string? LinksTitle, string? ContactTitle, string? Copyright);
