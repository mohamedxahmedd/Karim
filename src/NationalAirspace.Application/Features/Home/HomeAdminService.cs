using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Home;

/// <summary>Homepage management: hero slides, section blocks, statistics and important links.</summary>
public sealed class HomeAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "Homepage";

    // ---- hero slides ----

    public async Task<IReadOnlyList<HeroSlideAdminItem>> GetSlidesAsync(CancellationToken ct = default) =>
        (await Db.HeroSlides.AsNoTracking().OrderBy(x => x.DisplayOrder)
            .Select(x => new { x.Id, x.TitleAr, x.TitleEn, Path = x.Image != null ? x.Image.StoragePath : null, x.IsActive, x.DisplayOrder }).ToListAsync(ct))
        .Select(x => new HeroSlideAdminItem(x.Id, x.TitleAr, x.TitleEn, MediaUrls.Image(x.Path), x.IsActive, x.DisplayOrder)).ToList();

    public async Task<HeroSlideInput?> GetSlideAsync(Guid id, CancellationToken ct = default) =>
        await Db.HeroSlides.AsNoTracking().Where(x => x.Id == id).Select(x => new HeroSlideInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, TitleAr = x.TitleAr, TitleEn = x.TitleEn, SubtitleAr = x.SubtitleAr,
            SubtitleEn = x.SubtitleEn, DescriptionAr = x.DescriptionAr, DescriptionEn = x.DescriptionEn, ImageId = x.ImageId,
            PrimaryButtonTextAr = x.PrimaryButtonTextAr, PrimaryButtonTextEn = x.PrimaryButtonTextEn, PrimaryButtonUrl = x.PrimaryButtonUrl,
            SecondaryButtonTextAr = x.SecondaryButtonTextAr, SecondaryButtonTextEn = x.SecondaryButtonTextEn, SecondaryButtonUrl = x.SecondaryButtonUrl,
            IsActive = x.IsActive,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveSlideAsync(HeroSlideInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        if (!await MediaService.ExistsAsync(Db, input.ImageId, MediaKind.Image, ct)) errors.Add(new Error("Val.InvalidOption", nameof(input.ImageId)));
        var entity = input.Id is { } id ? await Db.HeroSlides.FirstOrDefaultAsync(x => x.Id == id, ct) : new HeroSlide();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.SubtitleAr = Clean(input.SubtitleAr);
        entity.SubtitleEn = Clean(input.SubtitleEn);
        entity.DescriptionAr = Clean(input.DescriptionAr);
        entity.DescriptionEn = Clean(input.DescriptionEn);
        entity.ImageId = input.ImageId;
        entity.PrimaryButtonTextAr = Clean(input.PrimaryButtonTextAr);
        entity.PrimaryButtonTextEn = Clean(input.PrimaryButtonTextEn);
        entity.PrimaryButtonUrl = Clean(input.PrimaryButtonUrl);
        entity.SecondaryButtonTextAr = Clean(input.SecondaryButtonTextAr);
        entity.SecondaryButtonTextEn = Clean(input.SecondaryButtonTextEn);
        entity.SecondaryButtonUrl = Clean(input.SecondaryButtonUrl);
        entity.IsActive = input.IsActive;
        if (input.Id is null)
        {
            entity.DisplayOrder = await NextOrderAsync(Db.HeroSlides, ct);
            Db.HeroSlides.Add(entity);
            Audit("CreateSlide", entity.Id, entity.TitleAr);
        }
        else
        {
            ExpectStamp(entity, input.ConcurrencyStamp);
            Audit("UpdateSlide", entity.Id, entity.TitleAr);
        }
        return await CommitAsync(entity.Id, ct);
    }

    public async Task<Result> ToggleSlideAsync(Guid id, CancellationToken ct = default)
    {
        var e = await Db.HeroSlides.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return Result.Failure(Error.NotFound);
        e.IsActive = !e.IsActive;
        Audit(e.IsActive ? "EnableSlide" : "DisableSlide", id, e.TitleAr);
        return await CommitAsync(ct);
    }

    public Task<Result> MoveSlideAsync(Guid id, int direction, CancellationToken ct = default) => MoveAsync(Db.HeroSlides, id, direction, ct);
    public Task<Result> DeleteSlideAsync(Guid id, CancellationToken ct = default) => DeleteAsync(Db.HeroSlides, id, x => x.TitleAr, ct);

    // ---- sections ----

    public async Task<IReadOnlyList<HomeSectionAdminItem>> GetSectionsAsync(CancellationToken ct = default) =>
        await Db.HomeSections.AsNoTracking().OrderBy(x => x.DisplayOrder)
            .Select(x => new HomeSectionAdminItem(x.Id, x.Key, x.TitleAr, x.TitleEn, x.IsVisible, x.DisplayOrder)).ToListAsync(ct);

    public async Task<HomeSectionInput?> GetSectionAsync(Guid id, CancellationToken ct = default) =>
        await Db.HomeSections.AsNoTracking().Where(x => x.Id == id).Select(x => new HomeSectionInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, Key = x.Key, TitleAr = x.TitleAr, TitleEn = x.TitleEn,
            SubtitleAr = x.SubtitleAr, SubtitleEn = x.SubtitleEn, DescriptionAr = x.DescriptionAr, DescriptionEn = x.DescriptionEn,
            ImageId = x.ImageId, ButtonTextAr = x.ButtonTextAr, ButtonTextEn = x.ButtonTextEn, ButtonUrl = x.ButtonUrl,
            ItemCount = x.ItemCount, IsVisible = x.IsVisible,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result> SaveSectionAsync(HomeSectionInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        if (!await MediaService.ExistsAsync(Db, input.ImageId, MediaKind.Image, ct)) errors.Add(new Error("Val.InvalidOption", nameof(input.ImageId)));
        var entity = input.Id is { } id ? await Db.HomeSections.FirstOrDefaultAsync(x => x.Id == id, ct) : null;
        if (entity is null) return Result.Failure(Error.NotFound);
        if (errors.Count > 0) return Result.Failure(errors);

        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.SubtitleAr = Clean(input.SubtitleAr);
        entity.SubtitleEn = Clean(input.SubtitleEn);
        entity.DescriptionAr = Clean(input.DescriptionAr);
        entity.DescriptionEn = Clean(input.DescriptionEn);
        entity.ImageId = input.ImageId;
        entity.ButtonTextAr = Clean(input.ButtonTextAr);
        entity.ButtonTextEn = Clean(input.ButtonTextEn);
        entity.ButtonUrl = Clean(input.ButtonUrl);
        entity.ItemCount = input.ItemCount;
        entity.IsVisible = input.IsVisible;
        ExpectStamp(entity, input.ConcurrencyStamp);
        Audit("UpdateSection", entity.Id, entity.Key.ToString());
        return await CommitAsync(ct);
    }

    public async Task<Result> ToggleSectionAsync(Guid id, CancellationToken ct = default)
    {
        var e = await Db.HomeSections.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null) return Result.Failure(Error.NotFound);
        e.IsVisible = !e.IsVisible;
        Audit(e.IsVisible ? "ShowSection" : "HideSection", id, e.Key.ToString());
        return await CommitAsync(ct);
    }

    public Task<Result> MoveSectionAsync(Guid id, int direction, CancellationToken ct = default) => MoveAsync(Db.HomeSections, id, direction, ct);

    // ---- statistics ----

    public async Task<IReadOnlyList<StatisticAdminItem>> GetStatisticsAsync(CancellationToken ct = default) =>
        await Db.Statistics.AsNoTracking().OrderBy(x => x.DisplayOrder)
            .Select(x => new StatisticAdminItem(x.Id, x.LabelAr, x.LabelEn, x.Value, x.Icon, x.IsActive, x.DisplayOrder)).ToListAsync(ct);

    public async Task<StatisticInput?> GetStatisticAsync(Guid id, CancellationToken ct = default) =>
        await Db.Statistics.AsNoTracking().Where(x => x.Id == id).Select(x => new StatisticInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, LabelAr = x.LabelAr, LabelEn = x.LabelEn, Value = x.Value, Icon = x.Icon, IsActive = x.IsActive,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveStatisticAsync(StatisticInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        var entity = input.Id is { } id ? await Db.Statistics.FirstOrDefaultAsync(x => x.Id == id, ct) : new Statistic();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        if (errors.Count > 0) return Result<Guid>.Failure(errors);
        entity.LabelAr = input.LabelAr.Trim();
        entity.LabelEn = input.LabelEn.Trim();
        entity.Value = input.Value.Trim();
        entity.Icon = Clean(input.Icon);
        entity.IsActive = input.IsActive;
        if (input.Id is null)
        {
            entity.DisplayOrder = await NextOrderAsync(Db.Statistics, ct);
            Db.Statistics.Add(entity);
            Audit("CreateStatistic", entity.Id, entity.LabelAr);
        }
        else
        {
            ExpectStamp(entity, input.ConcurrencyStamp);
            Audit("UpdateStatistic", entity.Id, entity.LabelAr);
        }
        return await CommitAsync(entity.Id, ct);
    }

    public Task<Result> MoveStatisticAsync(Guid id, int direction, CancellationToken ct = default) => MoveAsync(Db.Statistics, id, direction, ct);
    public Task<Result> DeleteStatisticAsync(Guid id, CancellationToken ct = default) => DeleteAsync(Db.Statistics, id, x => x.LabelAr, ct);

    // ---- important links ----

    public async Task<IReadOnlyList<ImportantLinkAdminItem>> GetLinksAsync(CancellationToken ct = default) =>
        await Db.ImportantLinks.AsNoTracking().OrderBy(x => x.DisplayOrder)
            .Select(x => new ImportantLinkAdminItem(x.Id, x.TitleAr, x.TitleEn, x.Url, x.Icon, x.IsActive, x.DisplayOrder)).ToListAsync(ct);

    public async Task<ImportantLinkInput?> GetLinkAsync(Guid id, CancellationToken ct = default) =>
        await Db.ImportantLinks.AsNoTracking().Where(x => x.Id == id).Select(x => new ImportantLinkInput
        {
            Id = x.Id, ConcurrencyStamp = x.ConcurrencyStamp, TitleAr = x.TitleAr, TitleEn = x.TitleEn, Url = x.Url, Icon = x.Icon, IsActive = x.IsActive,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveLinkAsync(ImportantLinkInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        var entity = input.Id is { } id ? await Db.ImportantLinks.FirstOrDefaultAsync(x => x.Id == id, ct) : new ImportantLink();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);
        if (errors.Count > 0) return Result<Guid>.Failure(errors);
        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.Url = input.Url.Trim();
        entity.Icon = Clean(input.Icon);
        entity.IsActive = input.IsActive;
        if (input.Id is null)
        {
            entity.DisplayOrder = await NextOrderAsync(Db.ImportantLinks, ct);
            Db.ImportantLinks.Add(entity);
            Audit("CreateLink", entity.Id, entity.TitleAr);
        }
        else
        {
            ExpectStamp(entity, input.ConcurrencyStamp);
            Audit("UpdateLink", entity.Id, entity.TitleAr);
        }
        return await CommitAsync(entity.Id, ct);
    }

    public Task<Result> MoveLinkAsync(Guid id, int direction, CancellationToken ct = default) => MoveAsync(Db.ImportantLinks, id, direction, ct);
    public Task<Result> DeleteLinkAsync(Guid id, CancellationToken ct = default) => DeleteAsync(Db.ImportantLinks, id, x => x.TitleAr, ct);
}
