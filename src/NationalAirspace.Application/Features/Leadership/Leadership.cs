using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Leadership;

public class LeaderInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string NameAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string NameEn { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string PositionAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string PositionEn { get; set; } = string.Empty;
    public string? BiographyAr { get; set; }
    public string? BiographyEn { get; set; }
    public Guid? PhotoId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record LeaderAdminListItem(Guid Id, string NameAr, string NameEn, string PositionAr, string PositionEn, string? PhotoUrl, bool IsActive, int DisplayOrder);

public sealed record LeaderDto(Guid Id, string Name, string Position, string? BiographyHtml, ImageRef? Photo);

public sealed class LeaderAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "Leader";

    public async Task<IReadOnlyList<LeaderAdminListItem>> ListAsync(CancellationToken ct = default) =>
        (await Db.Leaders.AsNoTracking().OrderBy(x => x.DisplayOrder)
            .Select(x => new { x.Id, x.NameAr, x.NameEn, x.PositionAr, x.PositionEn, Photo = x.Photo != null ? x.Photo.StoragePath : null, x.IsActive, x.DisplayOrder })
            .ToListAsync(ct))
        .Select(x => new LeaderAdminListItem(x.Id, x.NameAr, x.NameEn, x.PositionAr, x.PositionEn, MediaUrls.Image(x.Photo), x.IsActive, x.DisplayOrder)).ToList();

    public async Task<LeaderInput?> GetForEditAsync(Guid id, CancellationToken ct = default) =>
        await Db.Leaders.AsNoTracking().Where(x => x.Id == id).Select(x => new LeaderInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, NameAr = x.NameAr, NameEn = x.NameEn, PositionAr = x.PositionAr,
            PositionEn = x.PositionEn, BiographyAr = x.BiographyAr, BiographyEn = x.BiographyEn, PhotoId = x.PhotoId, IsActive = x.IsActive,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveAsync(LeaderInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        if (!await MediaService.ExistsAsync(Db, input.PhotoId, MediaKind.Image, ct)) errors.Add(new Error("Val.InvalidOption", nameof(input.PhotoId)));
        var entity = input.Id is { } id ? await Db.Leaders.FirstOrDefaultAsync(x => x.Id == id, ct) : new Leader();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        entity.NameAr = input.NameAr.Trim();
        entity.NameEn = input.NameEn.Trim();
        entity.PositionAr = input.PositionAr.Trim();
        entity.PositionEn = input.PositionEn.Trim();
        entity.BiographyAr = SanitizeHtml(input.BiographyAr);
        entity.BiographyEn = SanitizeHtml(input.BiographyEn);
        entity.PhotoId = input.PhotoId;
        entity.IsActive = input.IsActive;
        if (input.Id is null)
        {
            entity.DisplayOrder = await NextOrderAsync(Db.Leaders, ct);
            Db.Leaders.Add(entity);
            Audit("Create", entity.Id, entity.NameAr);
        }
        else
        {
            ExpectStamp(entity, input.ConcurrencyStamp);
            Audit("Update", entity.Id, entity.NameAr);
        }
        return await CommitAsync(entity.Id, ct);
    }

    public async Task<Result> ToggleActiveAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await Db.Leaders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return Result.Failure(Error.NotFound);
        entity.IsActive = !entity.IsActive;
        Audit(entity.IsActive ? "Activate" : "Deactivate", id, entity.NameAr);
        return await CommitAsync(ct);
    }

    public Task<Result> MoveAsync(Guid id, int direction, CancellationToken ct = default) => MoveAsync(Db.Leaders, id, direction, ct);

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) => DeleteAsync(Db.Leaders, id, x => x.NameAr, ct);
}

public sealed class LeaderPublicService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<LeaderDto>> GetActiveAsync(Lang lang, CancellationToken ct = default) =>
        (await db.Leaders.AsNoTracking().Where(x => x.IsActive).Include(x => x.Photo).OrderBy(x => x.DisplayOrder).ToListAsync(ct))
        .Select(x =>
        {
            var name = lang.Pick(x.NameAr, x.NameEn);
            return new LeaderDto(x.Id, name, lang.Pick(x.PositionAr, x.PositionEn), lang.PickOrNull(x.BiographyAr, x.BiographyEn), ImageRef.From(x.Photo, lang, name));
        }).ToList();
}
