using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Services;

public sealed class ServiceAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "Service";

    public async Task<PagedResult<ServiceAdminListItem>> ListAsync(AdminListQuery q, CancellationToken ct = default)
    {
        var query = Db.CenterServices.AsNoTracking();
        if (q.Term is { } term) query = query.Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term));
        if (q.Status is { } status) query = query.Where(x => x.Status == status);
        return await query.OrderBy(x => x.DisplayOrder)
            .Select(x => new ServiceAdminListItem(x.Id, x.TitleAr, x.TitleEn, x.Icon, x.Status, x.DisplayOrder, x.UpdatedAtUtc))
            .ToPagedAsync(q.Page, q.PageSize, ct);
    }

    public async Task<ServiceInput?> GetForEditAsync(Guid id, CancellationToken ct = default) =>
        await Db.CenterServices.AsNoTracking().Where(x => x.Id == id).Select(x => new ServiceInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, TitleAr = x.TitleAr, TitleEn = x.TitleEn,
            ShortDescriptionAr = x.ShortDescriptionAr, ShortDescriptionEn = x.ShortDescriptionEn,
            ContentAr = x.ContentAr, ContentEn = x.ContentEn, Icon = x.Icon, CoverImageId = x.CoverImageId,
            Status = x.Status, SlugAr = x.SlugAr, SlugEn = x.SlugEn,
            SeoTitleAr = x.SeoTitleAr, SeoTitleEn = x.SeoTitleEn, MetaDescriptionAr = x.MetaDescriptionAr, MetaDescriptionEn = x.MetaDescriptionEn,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveAsync(ServiceInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        var entity = input.Id is { } id ? await Db.CenterServices.FirstOrDefaultAsync(x => x.Id == id, ct) : new CenterService();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        if (!await MediaService.ExistsAsync(Db, input.CoverImageId, MediaKind.Image, ct))
            errors.Add(new Error("Val.InvalidOption", nameof(input.CoverImageId)));
        var slugs = await ResolveSlugsAsync(Db.CenterServices, entity.Id, input.SlugAr, input.SlugEn, input.TitleAr, input.TitleEn, errors, ct);
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.ShortDescriptionAr = Clean(input.ShortDescriptionAr);
        entity.ShortDescriptionEn = Clean(input.ShortDescriptionEn);
        entity.ContentAr = SanitizeHtml(input.ContentAr);
        entity.ContentEn = SanitizeHtml(input.ContentEn);
        entity.Icon = Clean(input.Icon);
        entity.CoverImageId = input.CoverImageId;
        entity.SlugAr = slugs.Ar;
        entity.SlugEn = slugs.En;
        entity.SeoTitleAr = Clean(input.SeoTitleAr);
        entity.SeoTitleEn = Clean(input.SeoTitleEn);
        entity.MetaDescriptionAr = Clean(input.MetaDescriptionAr);
        entity.MetaDescriptionEn = Clean(input.MetaDescriptionEn);
        entity.ChangeStatus(input.Status, Clock.UtcNow);

        if (input.Id is null)
        {
            entity.DisplayOrder = await NextOrderAsync(Db.CenterServices, ct);
            Db.CenterServices.Add(entity);
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
        ChangeStatusAsync(Db.CenterServices, id, status, x => x.TitleAr, ct);

    public Task<Result> MoveAsync(Guid id, int direction, CancellationToken ct = default) =>
        MoveAsync(Db.CenterServices, id, direction, ct);

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync(Db.CenterServices, id, x => x.TitleAr, ct);
}

public sealed class ServicePublicService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<ServiceCardDto>> GetAllAsync(Lang lang, int? take = null, CancellationToken ct = default)
    {
        IQueryable<CenterService> query = db.CenterServices.AsNoTracking().Published().Include(x => x.CoverImage).OrderBy(x => x.DisplayOrder);
        if (take is { } t) query = query.Take(t);
        return (await query.ToListAsync(ct)).Select(x => ToCard(x, lang)).ToList();
    }

    public async Task<ServiceDetailDto?> GetBySlugAsync(Lang lang, string slug, CancellationToken ct = default)
    {
        var x = await db.CenterServices.AsNoTracking().Published().Include(s => s.CoverImage)
            .FirstOrDefaultAsync(s => s.SlugAr == slug || s.SlugEn == slug, ct);
        if (x is null) return null;
        var others = await db.CenterServices.AsNoTracking().Published().Include(s => s.CoverImage)
            .Where(s => s.Id != x.Id).OrderBy(s => s.DisplayOrder).ToListAsync(ct);
        var title = lang.Pick(x.TitleAr, x.TitleEn);
        return new ServiceDetailDto(x.Id, title, lang.PickOrNull(x.ShortDescriptionAr, x.ShortDescriptionEn),
            lang.PickOrNull(x.ContentAr, x.ContentEn), x.Icon, ImageRef.From(x.CoverImage, lang, title), new SlugPair(x.SlugAr, x.SlugEn),
            SeoMeta.From(lang, x.SeoTitleAr, x.SeoTitleEn, x.MetaDescriptionAr ?? x.ShortDescriptionAr, x.MetaDescriptionEn ?? x.ShortDescriptionEn),
            others.Select(o => ToCard(o, lang)).ToList());
    }

    private static ServiceCardDto ToCard(CenterService x, Lang lang)
    {
        var title = lang.Pick(x.TitleAr, x.TitleEn);
        return new ServiceCardDto(x.Id, title, lang.PickOrNull(x.ShortDescriptionAr, x.ShortDescriptionEn), x.Icon,
            lang == Lang.Ar ? x.SlugAr : x.SlugEn, ImageRef.From(x.CoverImage, lang, title));
    }
}
