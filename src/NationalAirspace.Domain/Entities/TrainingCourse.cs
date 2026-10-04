using NationalAirspace.Domain.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Domain.Entities;

public class TrainingCourse : PublishableEntity, ISluggable
{
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string SlugAr { get; set; } = string.Empty;
    public string SlugEn { get; set; } = string.Empty;
    public string? DurationAr { get; set; }
    public string? DurationEn { get; set; }
    public string? TargetAudienceAr { get; set; }
    public string? TargetAudienceEn { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? LocationAr { get; set; }
    public string? LocationEn { get; set; }
    public RegistrationStatus RegistrationStatus { get; set; } = RegistrationStatus.ComingSoon;
    public Guid? CourseImageId { get; set; }
    public MediaFile? CourseImage { get; set; }
    public Guid? AttachmentId { get; set; }
    public MediaFile? Attachment { get; set; }
}
