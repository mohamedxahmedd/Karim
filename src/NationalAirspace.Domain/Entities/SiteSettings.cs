using NationalAirspace.Domain.Common;

namespace NationalAirspace.Domain.Entities;

/// <summary>Singleton row holding site-wide, admin-editable configuration.</summary>
public class SiteSettings : AuditableEntity
{
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000001");

    public string SiteNameAr { get; set; } = string.Empty;
    public string SiteNameEn { get; set; } = string.Empty;
    public string? TaglineAr { get; set; }
    public string? TaglineEn { get; set; }
    public string? AddressAr { get; set; }
    public string? AddressEn { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? WorkingHoursAr { get; set; }
    public string? WorkingHoursEn { get; set; }
    /// <summary>https embed URL (Google Maps / OpenStreetMap) for the contact page map.</summary>
    public string? MapEmbedUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? FacebookUrl { get; set; }
    public string? XUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? YouTubeUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? FooterTextAr { get; set; }
    public string? FooterTextEn { get; set; }
    public string? DefaultMetaDescriptionAr { get; set; }
    public string? DefaultMetaDescriptionEn { get; set; }
}
