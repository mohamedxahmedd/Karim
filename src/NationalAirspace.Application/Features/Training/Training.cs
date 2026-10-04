using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Training;

public class TrainingInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleEn { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    [Slug, StringLength(150, ErrorMessage = V.MaxLength)] public string? SlugAr { get; set; }
    [Slug, StringLength(150, ErrorMessage = V.MaxLength)] public string? SlugEn { get; set; }
    [StringLength(100, ErrorMessage = V.MaxLength)] public string? DurationAr { get; set; }
    [StringLength(100, ErrorMessage = V.MaxLength)] public string? DurationEn { get; set; }
    [StringLength(1000, ErrorMessage = V.MaxLength)] public string? TargetAudienceAr { get; set; }
    [StringLength(1000, ErrorMessage = V.MaxLength)] public string? TargetAudienceEn { get; set; }
    [DataType(DataType.Date)] public DateTime? StartDate { get; set; }
    [DataType(DataType.Date)] public DateTime? EndDate { get; set; }
    [StringLength(200, ErrorMessage = V.MaxLength)] public string? LocationAr { get; set; }
    [StringLength(200, ErrorMessage = V.MaxLength)] public string? LocationEn { get; set; }
    public RegistrationStatus RegistrationStatus { get; set; } = RegistrationStatus.ComingSoon;
    public Guid? CourseImageId { get; set; }
    public Guid? AttachmentId { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
}

public sealed record TrainingAdminListItem(Guid Id, string TitleAr, string TitleEn, ContentStatus Status, RegistrationStatus RegistrationStatus,
    DateTime? StartDate, DateTime? EndDate, DateTime UpdatedAtUtc);

public sealed record TrainingCardDto(Guid Id, string Title, string? Summary, string Slug, string? Duration, string? Location,
    DateTime? StartDate, DateTime? EndDate, RegistrationStatus RegistrationStatus, ImageRef? Image);

public sealed record TrainingDetailDto(Guid Id, string Title, string? DescriptionHtml, SlugPair Slugs, string? Duration, string? TargetAudience,
    string? Location, DateTime? StartDate, DateTime? EndDate, RegistrationStatus RegistrationStatus, ImageRef? Image,
    string? AttachmentName, long? AttachmentSize);

public sealed class TrainingAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "Training";

    public async Task<PagedResult<TrainingAdminListItem>> ListAsync(AdminListQuery q, CancellationToken ct = default)
    {
        var query = Db.TrainingCourses.AsNoTracking();
        if (q.Term is { } term) query = query.Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term));
        if (q.Status is { } status) query = query.Where(x => x.Status == status);
        return await query.OrderByDescending(x => x.StartDate ?? x.CreatedAtUtc)
            .Select(x => new TrainingAdminListItem(x.Id, x.TitleAr, x.TitleEn, x.Status, x.RegistrationStatus, x.StartDate, x.EndDate, x.UpdatedAtUtc))
            .ToPagedAsync(q.Page, q.PageSize, ct);
    }

    public async Task<TrainingInput?> GetForEditAsync(Guid id, CancellationToken ct = default) =>
        await Db.TrainingCourses.AsNoTracking().Where(x => x.Id == id).Select(x => new TrainingInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, TitleAr = x.TitleAr, TitleEn = x.TitleEn,
            DescriptionAr = x.DescriptionAr, DescriptionEn = x.DescriptionEn, SlugAr = x.SlugAr, SlugEn = x.SlugEn,
            DurationAr = x.DurationAr, DurationEn = x.DurationEn, TargetAudienceAr = x.TargetAudienceAr, TargetAudienceEn = x.TargetAudienceEn,
            StartDate = x.StartDate, EndDate = x.EndDate, LocationAr = x.LocationAr, LocationEn = x.LocationEn,
            RegistrationStatus = x.RegistrationStatus, CourseImageId = x.CourseImageId, AttachmentId = x.AttachmentId, Status = x.Status,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveAsync(TrainingInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        var entity = input.Id is { } id ? await Db.TrainingCourses.FirstOrDefaultAsync(x => x.Id == id, ct) : new TrainingCourse();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        if (input.StartDate is { } s && input.EndDate is { } e && e.Date < s.Date)
            errors.Add(new Error("Training.EndBeforeStart", nameof(input.EndDate)));
        if (!await MediaService.ExistsAsync(Db, input.CourseImageId, MediaKind.Image, ct))
            errors.Add(new Error("Val.InvalidOption", nameof(input.CourseImageId)));
        if (!await MediaService.ExistsAsync(Db, input.AttachmentId, MediaKind.Document, ct))
            errors.Add(new Error("Val.InvalidOption", nameof(input.AttachmentId)));
        var slugs = await ResolveSlugsAsync(Db.TrainingCourses, entity.Id, input.SlugAr, input.SlugEn, input.TitleAr, input.TitleEn, errors, ct);
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.DescriptionAr = SanitizeHtml(input.DescriptionAr);
        entity.DescriptionEn = SanitizeHtml(input.DescriptionEn);
        entity.SlugAr = slugs.Ar;
        entity.SlugEn = slugs.En;
        entity.DurationAr = Clean(input.DurationAr);
        entity.DurationEn = Clean(input.DurationEn);
        entity.TargetAudienceAr = Clean(input.TargetAudienceAr);
        entity.TargetAudienceEn = Clean(input.TargetAudienceEn);
        entity.StartDate = input.StartDate?.AsUtcDate();
        entity.EndDate = input.EndDate?.AsUtcDate();
        entity.LocationAr = Clean(input.LocationAr);
        entity.LocationEn = Clean(input.LocationEn);
        entity.RegistrationStatus = input.RegistrationStatus;
        entity.CourseImageId = input.CourseImageId;
        entity.AttachmentId = input.AttachmentId;
        entity.ChangeStatus(input.Status, Clock.UtcNow);

        if (input.Id is null)
        {
            Db.TrainingCourses.Add(entity);
            Audit("Create", entity.Id, entity.TitleAr);
        }
        else
        {
            ExpectStamp(entity, input.ConcurrencyStamp);
            Audit("Update", entity.Id, entity.TitleAr);
        }
        return await CommitAsync(entity.Id, ct);
    }

    public Task<Result> ChangeStatusAsync(Guid id, ContentStatus status, CancellationToken ct = default) =>
        ChangeStatusAsync(Db.TrainingCourses, id, status, x => x.TitleAr, ct);

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync(Db.TrainingCourses, id, x => x.TitleAr, ct);
}

public sealed class TrainingPublicService(IApplicationDbContext db, IClock clock)
{
    private IQueryable<TrainingCourse> Visible() => db.TrainingCourses.AsNoTracking().Published().Include(x => x.CourseImage);

    /// <summary>Upcoming/ongoing courses first (by start date), then undated, then past.</summary>
    public async Task<IReadOnlyList<TrainingCardDto>> GetUpcomingAsync(Lang lang, int count, CancellationToken ct = default)
    {
        var today = clock.UtcNow.Date;
        var items = await Visible().Where(x => x.EndDate == null || x.EndDate >= today)
            .OrderBy(x => x.StartDate == null).ThenBy(x => x.StartDate).Take(Math.Clamp(count, 1, 24)).ToListAsync(ct);
        return items.Select(x => ToCard(x, lang)).ToList();
    }

    public async Task<PagedResult<TrainingCardDto>> SearchAsync(Lang lang, string? search, bool past, int page, int pageSize = 9, CancellationToken ct = default)
    {
        var today = clock.UtcNow.Date;
        var query = Visible();
        query = past ? query.Where(x => x.EndDate != null && x.EndDate < today) : query.Where(x => x.EndDate == null || x.EndDate >= today);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term));
        }
        query = past ? query.OrderByDescending(x => x.EndDate) : query.OrderBy(x => x.StartDate == null).ThenBy(x => x.StartDate);
        var paged = await query.ToPagedAsync(page, pageSize, ct);
        return paged.Map(x => ToCard(x, lang));
    }

    public async Task<TrainingDetailDto?> GetBySlugAsync(Lang lang, string slug, CancellationToken ct = default)
    {
        var x = await Visible().Include(c => c.Attachment).FirstOrDefaultAsync(c => c.SlugAr == slug || c.SlugEn == slug, ct);
        if (x is null) return null;
        var title = lang.Pick(x.TitleAr, x.TitleEn);
        return new TrainingDetailDto(x.Id, title, lang.PickOrNull(x.DescriptionAr, x.DescriptionEn), new SlugPair(x.SlugAr, x.SlugEn),
            lang.PickOrNull(x.DurationAr, x.DurationEn), lang.PickOrNull(x.TargetAudienceAr, x.TargetAudienceEn),
            lang.PickOrNull(x.LocationAr, x.LocationEn), x.StartDate, x.EndDate, x.RegistrationStatus,
            ImageRef.From(x.CourseImage, lang, title), x.Attachment?.OriginalFileName, x.Attachment?.SizeBytes);
    }

    public async Task<Guid?> GetAttachmentIdAsync(string slug, CancellationToken ct = default) =>
        await db.TrainingCourses.AsNoTracking().Published().Where(c => c.SlugAr == slug || c.SlugEn == slug)
            .Select(c => c.AttachmentId).FirstOrDefaultAsync(ct);

    private static TrainingCardDto ToCard(TrainingCourse x, Lang lang)
    {
        var title = lang.Pick(x.TitleAr, x.TitleEn);
        return new TrainingCardDto(x.Id, title, lang.PickOrNull(x.TargetAudienceAr, x.TargetAudienceEn), lang == Lang.Ar ? x.SlugAr : x.SlugEn,
            lang.PickOrNull(x.DurationAr, x.DurationEn), lang.PickOrNull(x.LocationAr, x.LocationEn), x.StartDate, x.EndDate,
            x.RegistrationStatus, ImageRef.From(x.CourseImage, lang, title));
    }
}
