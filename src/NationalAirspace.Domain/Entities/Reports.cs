using NationalAirspace.Domain.Common;

namespace NationalAirspace.Domain.Entities;

public class ReportCategory : AuditableEntity, IOrderable
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}

public class Report : PublishableEntity, ISluggable
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string SlugAr { get; set; } = string.Empty;
    public string SlugEn { get; set; } = string.Empty;
    public Guid? ReportCategoryId { get; set; }
    public ReportCategory? ReportCategory { get; set; }
    /// <summary>Publication date (date part only, stored as UTC midnight).</summary>
    public DateTime PublicationDate { get; set; }
    public Guid? PdfFileId { get; set; }
    public MediaFile? PdfFile { get; set; }
    public Guid? CoverImageId { get; set; }
    public MediaFile? CoverImage { get; set; }
    public int DownloadCount { get; set; }
    public string? SeoTitleAr { get; set; }
    public string? SeoTitleEn { get; set; }
    public string? MetaDescriptionAr { get; set; }
    public string? MetaDescriptionEn { get; set; }
}
