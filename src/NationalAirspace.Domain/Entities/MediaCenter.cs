using NationalAirspace.Domain.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Domain.Entities;

public class PhotoAlbum : PublishableEntity, IOrderable
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public DateTime? EventDate { get; set; }
    public Guid? CoverMediaId { get; set; }
    public MediaFile? CoverMedia { get; set; }
    public int DisplayOrder { get; set; }
    public ICollection<AlbumPhoto> Photos { get; set; } = new List<AlbumPhoto>();
}

public class AlbumPhoto : Entity, IOrderable
{
    public Guid AlbumId { get; set; }
    public PhotoAlbum? Album { get; set; }
    public Guid MediaFileId { get; set; }
    public MediaFile? MediaFile { get; set; }
    public string? CaptionAr { get; set; }
    public string? CaptionEn { get; set; }
    public int DisplayOrder { get; set; }
}

public class Video : PublishableEntity, IOrderable
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string Url { get; set; } = string.Empty;
    public VideoProvider Provider { get; set; }
    public string? ExternalId { get; set; }
    public Guid? ThumbnailId { get; set; }
    public MediaFile? Thumbnail { get; set; }
    public int DisplayOrder { get; set; }
}
