using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Legislation;

public class LegalDocumentInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(250, ErrorMessage = V.MaxLength)] public string TitleEn { get; set; } = string.Empty;
    [StringLength(4000, ErrorMessage = V.MaxLength)] public string? DescriptionAr { get; set; }
    [StringLength(4000, ErrorMessage = V.MaxLength)] public string? DescriptionEn { get; set; }
    public LegalDocumentCategory Category { get; set; }
    [StringLength(100, ErrorMessage = V.MaxLength)] public string? DocumentNumber { get; set; }
    [Required(ErrorMessage = V.Required), DataType(DataType.Date)] public DateTime? PublicationDate { get; set; }
    public Guid? DocumentFileId { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
}

public sealed record LegalDocumentAdminListItem(Guid Id, string TitleAr, string TitleEn, LegalDocumentCategory Category, string? DocumentNumber,
    DateTime PublicationDate, ContentStatus Status, bool HasFile);

public sealed record LegalDocumentDto(Guid Id, string Title, string? Description, LegalDocumentCategory Category, string? DocumentNumber,
    DateTime PublicationDate, string? FileName, long? FileSize);

public sealed class LegislationAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "LegalDocument";

    public async Task<PagedResult<LegalDocumentAdminListItem>> ListAsync(AdminListQuery q, LegalDocumentCategory? category, CancellationToken ct = default)
    {
        var query = Db.LegalDocuments.AsNoTracking();
        if (q.Term is { } term) query = query.Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term) || (x.DocumentNumber != null && x.DocumentNumber.Contains(term)));
        if (q.Status is { } status) query = query.Where(x => x.Status == status);
        if (category is { } c) query = query.Where(x => x.Category == c);
        return await query.OrderByDescending(x => x.PublicationDate)
            .Select(x => new LegalDocumentAdminListItem(x.Id, x.TitleAr, x.TitleEn, x.Category, x.DocumentNumber, x.PublicationDate, x.Status, x.DocumentFileId != null))
            .ToPagedAsync(q.Page, q.PageSize, ct);
    }

    public async Task<LegalDocumentInput?> GetForEditAsync(Guid id, CancellationToken ct = default) =>
        await Db.LegalDocuments.AsNoTracking().Where(x => x.Id == id).Select(x => new LegalDocumentInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, TitleAr = x.TitleAr, TitleEn = x.TitleEn,
            DescriptionAr = x.DescriptionAr, DescriptionEn = x.DescriptionEn, Category = x.Category, DocumentNumber = x.DocumentNumber,
            PublicationDate = x.PublicationDate, DocumentFileId = x.DocumentFileId, Status = x.Status,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveAsync(LegalDocumentInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        var entity = input.Id is { } id ? await Db.LegalDocuments.FirstOrDefaultAsync(x => x.Id == id, ct) : new LegalDocument();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        if (!Enum.IsDefined(input.Category)) errors.Add(new Error("Val.InvalidOption", nameof(input.Category)));
        if (!await MediaService.ExistsAsync(Db, input.DocumentFileId, MediaKind.Document, ct))
            errors.Add(new Error("Val.InvalidOption", nameof(input.DocumentFileId)));
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.DescriptionAr = Clean(input.DescriptionAr);
        entity.DescriptionEn = Clean(input.DescriptionEn);
        entity.Category = input.Category;
        entity.DocumentNumber = Clean(input.DocumentNumber);
        entity.PublicationDate = input.PublicationDate!.Value.AsUtcDate();
        entity.DocumentFileId = input.DocumentFileId;
        entity.ChangeStatus(input.Status, Clock.UtcNow);

        if (input.Id is null)
        {
            Db.LegalDocuments.Add(entity);
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
        ChangeStatusAsync(Db.LegalDocuments, id, status, x => x.TitleAr, ct);

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync(Db.LegalDocuments, id, x => x.TitleAr, ct);
}

public sealed class LegislationPublicService(IApplicationDbContext db)
{
    public async Task<PagedResult<LegalDocumentDto>> SearchAsync(Lang lang, string? search, LegalDocumentCategory? category, int page, int pageSize = 12, CancellationToken ct = default)
    {
        var query = db.LegalDocuments.AsNoTracking().Published().Include(x => x.DocumentFile).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term)
                                  || (x.DescriptionAr != null && x.DescriptionAr.Contains(term))
                                  || (x.DescriptionEn != null && x.DescriptionEn.Contains(term))
                                  || (x.DocumentNumber != null && x.DocumentNumber.Contains(term)));
        }
        if (category is { } c) query = query.Where(x => x.Category == c);
        var paged = await query.OrderByDescending(x => x.PublicationDate).ToPagedAsync(page, pageSize, ct);
        return paged.Map(x => new LegalDocumentDto(x.Id, lang.Pick(x.TitleAr, x.TitleEn), lang.PickOrNull(x.DescriptionAr, x.DescriptionEn),
            x.Category, x.DocumentNumber, x.PublicationDate, x.DocumentFile?.OriginalFileName, x.DocumentFile?.SizeBytes));
    }

    public async Task<Guid?> GetDocumentFileIdAsync(Guid id, CancellationToken ct = default) =>
        await db.LegalDocuments.AsNoTracking().Published().Where(x => x.Id == id).Select(x => x.DocumentFileId).FirstOrDefaultAsync(ct);
}
