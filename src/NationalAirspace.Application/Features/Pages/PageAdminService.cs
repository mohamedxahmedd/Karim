using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Pages;

public sealed class PageAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "Page";

    public async Task<PagedResult<PageAdminListItem>> ListAsync(AdminListQuery q, CancellationToken ct = default)
    {
        var query = Db.ContentPages.AsNoTracking();
        if (q.Term is { } term) query = query.Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term));
        if (q.Status is { } status) query = query.Where(x => x.Status == status);
        return await query.OrderBy(x => x.SystemKey == null).ThenBy(x => x.TitleEn)
            .Select(x => new PageAdminListItem(x.Id, x.TitleAr, x.TitleEn, x.SystemKey, x.Status, x.Sections.Count, x.UpdatedAtUtc))
            .ToPagedAsync(q.Page, q.PageSize, ct);
    }

    public async Task<PageInput?> GetForEditAsync(Guid id, CancellationToken ct = default) =>
        await Db.ContentPages.AsNoTracking().Where(x => x.Id == id).Select(x => new PageInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, TitleAr = x.TitleAr, TitleEn = x.TitleEn,
            SlugAr = x.SlugAr, SlugEn = x.SlugEn, SummaryAr = x.SummaryAr, SummaryEn = x.SummaryEn,
            ContentAr = x.ContentAr, ContentEn = x.ContentEn, HeroImageId = x.HeroImageId, Status = x.Status,
            SeoTitleAr = x.SeoTitleAr, SeoTitleEn = x.SeoTitleEn, MetaDescriptionAr = x.MetaDescriptionAr, MetaDescriptionEn = x.MetaDescriptionEn,
            SystemKey = x.SystemKey,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveAsync(PageInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        var entity = input.Id is { } id ? await Db.ContentPages.FirstOrDefaultAsync(x => x.Id == id, ct) : new ContentPage();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        if (!await MediaService.ExistsAsync(Db, input.HeroImageId, MediaKind.Image, ct))
            errors.Add(new Error("Val.InvalidOption", nameof(input.HeroImageId)));
        var slugs = await ResolveSlugsAsync(Db.ContentPages, entity.Id, input.SlugAr, input.SlugEn, input.TitleAr, input.TitleEn, errors, ct);
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.SlugAr = slugs.Ar;
        entity.SlugEn = slugs.En;
        entity.SummaryAr = Clean(input.SummaryAr);
        entity.SummaryEn = Clean(input.SummaryEn);
        entity.ContentAr = SanitizeHtml(input.ContentAr);
        entity.ContentEn = SanitizeHtml(input.ContentEn);
        entity.HeroImageId = input.HeroImageId;
        entity.SeoTitleAr = Clean(input.SeoTitleAr);
        entity.SeoTitleEn = Clean(input.SeoTitleEn);
        entity.MetaDescriptionAr = Clean(input.MetaDescriptionAr);
        entity.MetaDescriptionEn = Clean(input.MetaDescriptionEn);
        entity.ChangeStatus(input.Status, Clock.UtcNow);

        if (input.Id is null)
        {
            Db.ContentPages.Add(entity);
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
        ChangeStatusAsync(Db.ContentPages, id, status, x => x.TitleAr, ct);

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var page = await Db.ContentPages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (page is null) return Result.Failure(Error.NotFound);
        if (page.IsSystemPage) return Result.Failure(new Error("Pages.SystemPageDelete"));
        return await DeleteAsync(Db.ContentPages, id, x => x.TitleAr, ct);
    }

    // ---- sections ----

    public async Task<IReadOnlyList<PageSectionAdminItem>> GetSectionsAsync(Guid pageId, CancellationToken ct = default) =>
        await Db.PageSections.AsNoTracking().Where(s => s.PageId == pageId).OrderBy(s => s.DisplayOrder)
            .Select(s => new PageSectionAdminItem(s.Id, s.Key, s.Layout, s.TitleAr, s.TitleEn, s.IsVisible, s.DisplayOrder))
            .ToListAsync(ct);

    public async Task<PageSectionInput?> GetSectionForEditAsync(Guid id, CancellationToken ct = default) =>
        await Db.PageSections.AsNoTracking().Where(s => s.Id == id).Select(s => new PageSectionInput
        {
            Id = s.Id, ConcurrencyStamp = s.ConcurrencyStamp, PageId = s.PageId, Key = s.Key, Layout = s.Layout,
            TitleAr = s.TitleAr, TitleEn = s.TitleEn, ContentAr = s.ContentAr, ContentEn = s.ContentEn,
            Icon = s.Icon, ImageId = s.ImageId, IsVisible = s.IsVisible,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveSectionAsync(PageSectionInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        if (!await Db.ContentPages.AnyAsync(p => p.Id == input.PageId, ct)) return Result<Guid>.Failure(Error.NotFound);
        var entity = input.Id is { } id ? await Db.PageSections.FirstOrDefaultAsync(s => s.Id == id && s.PageId == input.PageId, ct) : new PageSection();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        if (!await MediaService.ExistsAsync(Db, input.ImageId, MediaKind.Image, ct))
            errors.Add(new Error("Val.InvalidOption", nameof(input.ImageId)));
        var key = Clean(input.Key);
        if (key is not null && await Db.PageSections.AnyAsync(s => s.PageId == input.PageId && s.Key == key && s.Id != entity.Id, ct))
            errors.Add(new Error("Pages.SectionKeyTaken", nameof(input.Key)));
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        entity.PageId = input.PageId;
        entity.Key = key;
        entity.Layout = input.Layout;
        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.ContentAr = SanitizeHtml(input.ContentAr);
        entity.ContentEn = SanitizeHtml(input.ContentEn);
        entity.Icon = Clean(input.Icon);
        entity.ImageId = input.ImageId;
        entity.IsVisible = input.IsVisible;

        if (input.Id is null)
        {
            entity.DisplayOrder = await NextOrderAsync(Db.PageSections.Where(s => s.PageId == input.PageId), ct);
            Db.PageSections.Add(entity);
            Audit("CreateSection", entity.Id, entity.TitleAr);
        }
        else
        {
            ExpectStamp(entity, input.ConcurrencyStamp);
            Audit("UpdateSection", entity.Id, entity.TitleAr);
        }
        return await CommitAsync(entity.Id, ct);
    }

    public async Task<Result> MoveSectionAsync(Guid id, int direction, CancellationToken ct = default)
    {
        var section = await Db.PageSections.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (section is null) return Result.Failure(Error.NotFound);
        return await MoveAsync(Db.PageSections.Where(s => s.PageId == section.PageId), id, direction, ct);
    }

    public async Task<Result> ToggleSectionAsync(Guid id, CancellationToken ct = default)
    {
        var section = await Db.PageSections.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (section is null) return Result.Failure(Error.NotFound);
        section.IsVisible = !section.IsVisible;
        Audit(section.IsVisible ? "ShowSection" : "HideSection", id, section.TitleAr);
        return await CommitAsync(ct);
    }

    public Task<Result> DeleteSectionAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync(Db.PageSections, id, x => x.TitleAr, ct);
}
