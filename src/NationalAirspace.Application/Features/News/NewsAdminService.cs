using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.News;

public sealed class NewsAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "News";

    public async Task<PagedResult<NewsAdminListItem>> ListAsync(AdminListQuery q, CancellationToken ct = default)
    {
        var query = Db.NewsArticles.AsNoTracking();
        if (q.Term is { } term) query = query.Where(x => x.TitleAr.Contains(term) || x.TitleEn.Contains(term));
        if (q.Status is { } status) query = query.Where(x => x.Status == status);
        if (q.CategoryId is { } cat) query = query.Where(x => x.NewsCategoryId == cat);
        var now = Clock.UtcNow;
        return await query
            .OrderByDescending(x => x.PublishDateUtc ?? x.CreatedAtUtc)
            .Select(x => new NewsAdminListItem(x.Id, x.TitleAr, x.TitleEn, x.Status,
                x.Status == ContentStatus.Published && x.PublishDateUtc > now, x.IsFeatured, x.PublishDateUtc,
                x.NewsCategory != null ? x.NewsCategory.NameAr : null, x.NewsCategory != null ? x.NewsCategory.NameEn : null,
                x.UpdatedAtUtc))
            .ToPagedAsync(q.Page, q.PageSize, ct);
    }

    public async Task<NewsInput?> GetForEditAsync(Guid id, CancellationToken ct = default)
    {
        var x = await Db.NewsArticles.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, ct);
        if (x is null) return null;
        return new NewsInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp,
            TitleAr = x.TitleAr, TitleEn = x.TitleEn, SummaryAr = x.SummaryAr, SummaryEn = x.SummaryEn,
            ContentAr = x.ContentAr, ContentEn = x.ContentEn, SlugAr = x.SlugAr, SlugEn = x.SlugEn,
            FeaturedImageId = x.FeaturedImageId, NewsCategoryId = x.NewsCategoryId,
            AuthorAr = x.AuthorAr, AuthorEn = x.AuthorEn, PublishDateLocal = Clock.ToSiteTime(x.PublishDateUtc),
            Status = x.Status, IsFeatured = x.IsFeatured,
            SeoTitleAr = x.SeoTitleAr, SeoTitleEn = x.SeoTitleEn, MetaDescriptionAr = x.MetaDescriptionAr, MetaDescriptionEn = x.MetaDescriptionEn,
        };
    }

    public Task<Result<Guid>> CreateAsync(NewsInput input, CancellationToken ct = default) => SaveAsync(input, null, ct);

    public async Task<Result> UpdateAsync(NewsInput input, CancellationToken ct = default)
    {
        if (input.Id is not { } id) return Result.Failure(Error.NotFound);
        return await SaveAsync(input, id, ct);
    }

    private async Task<Result<Guid>> SaveAsync(NewsInput input, Guid? id, CancellationToken ct)
    {
        var errors = InputValidator.Validate(input);
        NewsArticle? entity;
        if (id is { } existingId)
        {
            entity = await Db.NewsArticles.FirstOrDefaultAsync(x => x.Id == existingId, ct);
            if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        }
        else entity = new NewsArticle();

        if (input.NewsCategoryId is { } catId && !await Db.NewsCategories.AnyAsync(c => c.Id == catId, ct))
            errors.Add(new Error("Val.InvalidOption", nameof(input.NewsCategoryId)));
        if (!await MediaService.ExistsAsync(Db, input.FeaturedImageId, MediaKind.Image, ct))
            errors.Add(new Error("Val.InvalidOption", nameof(input.FeaturedImageId)));
        var slugs = await ResolveSlugsAsync(Db.NewsArticles, entity.Id, input.SlugAr, input.SlugEn, input.TitleAr, input.TitleEn, errors, ct);
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.SummaryAr = Clean(input.SummaryAr);
        entity.SummaryEn = Clean(input.SummaryEn);
        entity.ContentAr = SanitizeHtml(input.ContentAr);
        entity.ContentEn = SanitizeHtml(input.ContentEn);
        entity.SlugAr = slugs.Ar;
        entity.SlugEn = slugs.En;
        entity.FeaturedImageId = input.FeaturedImageId;
        entity.NewsCategoryId = input.NewsCategoryId;
        entity.AuthorAr = Clean(input.AuthorAr);
        entity.AuthorEn = Clean(input.AuthorEn);
        entity.IsFeatured = input.IsFeatured;
        entity.SeoTitleAr = Clean(input.SeoTitleAr);
        entity.SeoTitleEn = Clean(input.SeoTitleEn);
        entity.MetaDescriptionAr = Clean(input.MetaDescriptionAr);
        entity.MetaDescriptionEn = Clean(input.MetaDescriptionEn);
        entity.PublishDateUtc = Clock.FromSiteTime(input.PublishDateLocal);
        entity.ChangeStatus(input.Status, Clock.UtcNow);

        if (id is null)
        {
            Db.NewsArticles.Add(entity);
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
        ChangeStatusAsync(Db.NewsArticles, id, status, x => x.TitleAr, ct);

    public async Task<Result> ToggleFeaturedAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await Db.NewsArticles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return Result.Failure(Error.NotFound);
        entity.IsFeatured = !entity.IsFeatured;
        Audit(entity.IsFeatured ? "Feature" : "Unfeature", id, entity.TitleAr);
        return await CommitAsync(ct);
    }

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync(Db.NewsArticles, id, x => x.TitleAr, ct);

    public async Task<IReadOnlyList<CategoryOption>> GetCategoryOptionsAsync(CancellationToken ct = default) =>
        await Db.NewsCategories.AsNoTracking().OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryOption(c.Id, c.NameAr, c.NameEn)).ToListAsync(ct);
}

public sealed class NewsCategoryAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "NewsCategory";

    public async Task<IReadOnlyList<CategoryAdminItem>> ListAsync(CancellationToken ct = default) =>
        await Db.NewsCategories.AsNoTracking().OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryAdminItem(c.Id, c.NameAr, c.NameEn, c.Slug, c.IsActive, c.DisplayOrder, c.Articles.Count))
            .ToListAsync(ct);

    public async Task<NewsCategoryInput?> GetForEditAsync(Guid id, CancellationToken ct = default) =>
        await Db.NewsCategories.AsNoTracking().Where(c => c.Id == id)
            .Select(c => new NewsCategoryInput { Id = c.Id, ConcurrencyStamp = c.ConcurrencyStamp, NameAr = c.NameAr, NameEn = c.NameEn, Slug = c.Slug, IsActive = c.IsActive })
            .FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveAsync(NewsCategoryInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        NewsCategory? entity = input.Id is { } id ? await Db.NewsCategories.FirstOrDefaultAsync(c => c.Id == id, ct) : new NewsCategory();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);

        var slug = string.IsNullOrWhiteSpace(input.Slug) ? SlugGenerator.Generate(input.NameEn) : input.Slug.Trim().ToLowerInvariant();
        if (slug.Length == 0) slug = SlugGenerator.Generate(input.NameAr);
        if (await Db.NewsCategories.AnyAsync(c => c.Id != entity.Id && c.Slug == slug, ct))
            errors.Add(new Error(V.SlugTaken, nameof(input.Slug)));
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        entity.NameAr = input.NameAr.Trim();
        entity.NameEn = input.NameEn.Trim();
        entity.Slug = slug;
        entity.IsActive = input.IsActive;
        if (input.Id is null)
        {
            entity.DisplayOrder = await NextOrderAsync(Db.NewsCategories, ct);
            Db.NewsCategories.Add(entity);
            Audit("Create", entity.Id, entity.NameAr);
        }
        else
        {
            ExpectStamp(entity, input.ConcurrencyStamp);
            Audit("Update", entity.Id, entity.NameAr);
        }
        return await CommitAsync(entity.Id, ct);
    }

    public Task<Result> MoveAsync(Guid id, int direction, CancellationToken ct = default) =>
        MoveAsync(Db.NewsCategories, id, direction, ct);

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // Articles keep existing without a category (FK is SET NULL); load them so tracked state stays consistent.
        await Db.NewsArticles.Where(a => a.NewsCategoryId == id).LoadAsync(ct);
        return await DeleteAsync(Db.NewsCategories, id, x => x.NameAr, ct);
    }
}
