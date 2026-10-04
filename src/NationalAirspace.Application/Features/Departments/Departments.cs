using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Departments;

public class DepartmentInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string NameAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string NameEn { get; set; } = string.Empty;
    [StringLength(4000, ErrorMessage = V.MaxLength)] public string? DescriptionAr { get; set; }
    [StringLength(4000, ErrorMessage = V.MaxLength)] public string? DescriptionEn { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
}

public sealed record DepartmentAdminNode(Guid Id, Guid? ParentId, string NameAr, string NameEn, ContentStatus Status, int DisplayOrder, int Depth, IReadOnlyList<DepartmentAdminNode> Children);

public sealed record DepartmentOption(Guid Id, string NameAr, string NameEn, int Depth);

public sealed record DepartmentNodeDto(Guid Id, string Name, string? Description, IReadOnlyList<DepartmentNodeDto> Children);

public sealed class DepartmentAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "Department";

    public async Task<IReadOnlyList<DepartmentAdminNode>> GetTreeAsync(CancellationToken ct = default)
    {
        var all = await Db.Departments.AsNoTracking().OrderBy(d => d.DisplayOrder).ToListAsync(ct);
        IReadOnlyList<DepartmentAdminNode> Build(Guid? parent, int depth) => depth > 20 ? [] : all.Where(d => d.ParentDepartmentId == parent)
            .Select(d => new DepartmentAdminNode(d.Id, d.ParentDepartmentId, d.NameAr, d.NameEn, d.Status, d.DisplayOrder, depth, Build(d.Id, depth + 1)))
            .ToList();
        return Build(null, 0);
    }

    /// <summary>Flattened tree for parent dropdowns, excluding <paramref name="excludeId"/> and its descendants.</summary>
    public async Task<IReadOnlyList<DepartmentOption>> GetParentOptionsAsync(Guid? excludeId, CancellationToken ct = default)
    {
        var result = new List<DepartmentOption>();
        void Walk(IEnumerable<DepartmentAdminNode> nodes)
        {
            foreach (var n in nodes)
            {
                if (n.Id == excludeId) continue;
                result.Add(new DepartmentOption(n.Id, n.NameAr, n.NameEn, n.Depth));
                Walk(n.Children);
            }
        }
        Walk(await GetTreeAsync(ct));
        return result;
    }

    public async Task<DepartmentInput?> GetForEditAsync(Guid id, CancellationToken ct = default) =>
        await Db.Departments.AsNoTracking().Where(x => x.Id == id).Select(x => new DepartmentInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, ParentDepartmentId = x.ParentDepartmentId, NameAr = x.NameAr, NameEn = x.NameEn,
            DescriptionAr = x.DescriptionAr, DescriptionEn = x.DescriptionEn, Status = x.Status,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveAsync(DepartmentInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        var entity = input.Id is { } id ? await Db.Departments.FirstOrDefaultAsync(x => x.Id == id, ct) : new Department();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);

        if (input.ParentDepartmentId is { } parentId)
        {
            var parentMap = await Db.Departments.AsNoTracking().ToDictionaryAsync(d => d.Id, d => d.ParentDepartmentId, ct);
            if (!parentMap.ContainsKey(parentId) || Department.WouldCreateCycle(entity.Id, parentId, parentMap))
                errors.Add(new Error("Departments.InvalidParent", nameof(input.ParentDepartmentId)));
        }
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        var parentChanged = input.Id is null || entity.ParentDepartmentId != input.ParentDepartmentId;
        entity.ParentDepartmentId = input.ParentDepartmentId;
        entity.NameAr = input.NameAr.Trim();
        entity.NameEn = input.NameEn.Trim();
        entity.DescriptionAr = Clean(input.DescriptionAr);
        entity.DescriptionEn = Clean(input.DescriptionEn);
        entity.ChangeStatus(input.Status, Clock.UtcNow);
        if (parentChanged)
            entity.DisplayOrder = await NextOrderAsync(Db.Departments.Where(d => d.ParentDepartmentId == input.ParentDepartmentId), ct);

        if (input.Id is null)
        {
            Db.Departments.Add(entity);
            Audit("Create", entity.Id, entity.NameAr);
        }
        else
        {
            ExpectStamp(entity, input.ConcurrencyStamp);
            Audit("Update", entity.Id, entity.NameAr);
        }
        return await CommitAsync(entity.Id, ct);
    }

    public Task<Result> ChangeStatusAsync(Guid id, ContentStatus status, CancellationToken ct = default) =>
        ChangeStatusAsync(Db.Departments, id, status, x => x.NameAr, ct);

    public async Task<Result> MoveAsync(Guid id, int direction, CancellationToken ct = default)
    {
        var d = await Db.Departments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (d is null) return Result.Failure(Error.NotFound);
        return await MoveAsync(Db.Departments.Where(x => x.ParentDepartmentId == d.ParentDepartmentId), id, direction, ct);
    }

    /// <summary>Deletion is refused while the department still has sub-departments.</summary>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (await Db.Departments.AnyAsync(d => d.ParentDepartmentId == id, ct))
            return Result.Failure(new Error("Departments.HasChildren"));
        return await DeleteAsync(Db.Departments, id, x => x.NameAr, ct);
    }
}

public sealed class DepartmentPublicService(IApplicationDbContext db)
{
    /// <summary>Published hierarchy. A draft department hides its whole branch.</summary>
    public async Task<IReadOnlyList<DepartmentNodeDto>> GetTreeAsync(Lang lang, CancellationToken ct = default)
    {
        var all = await db.Departments.AsNoTracking().Published().OrderBy(d => d.DisplayOrder).ToListAsync(ct);
        IReadOnlyList<DepartmentNodeDto> Build(Guid? parent, int depth) => depth > 20 ? [] : all.Where(d => d.ParentDepartmentId == parent)
            .Select(d => new DepartmentNodeDto(d.Id, lang.Pick(d.NameAr, d.NameEn), lang.PickOrNull(d.DescriptionAr, d.DescriptionEn), Build(d.Id, depth + 1)))
            .ToList();
        return Build(null, 0);
    }
}
