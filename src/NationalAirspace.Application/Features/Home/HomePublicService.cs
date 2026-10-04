using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.News;
using NationalAirspace.Application.Features.Pages;
using NationalAirspace.Application.Features.Reports;
using NationalAirspace.Application.Features.Services;
using NationalAirspace.Application.Features.Training;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Home;

/// <summary>Builds the homepage from its admin-managed blocks; only visible blocks load their data.</summary>
public sealed class HomePublicService(
    IApplicationDbContext db,
    PagePublicService pages,
    ServicePublicService services,
    NewsPublicService news,
    ReportPublicService reports,
    TrainingPublicService training)
{
    public async Task<HomePageDto> GetAsync(Lang lang, CancellationToken ct = default)
    {
        var slides = (await db.HeroSlides.AsNoTracking().Where(s => s.IsActive).Include(s => s.Image).OrderBy(s => s.DisplayOrder).ToListAsync(ct))
            .Select(s => ToSlide(s, lang)).ToList();

        var blocks = (await db.HomeSections.AsNoTracking().Where(s => s.IsVisible).Include(s => s.Image).OrderBy(s => s.DisplayOrder).ToListAsync(ct))
            .Select(s => ToBlock(s, lang)).ToList();

        int? CountFor(HomeSectionKey key) => blocks.FirstOrDefault(b => b.Key == key)?.ItemCount;

        IReadOnlyList<PageSectionDto> visionMission = [], responsibilities = [], atfmSteps = [];
        IReadOnlyList<ServiceCardDto> serviceCards = [];
        IReadOnlyList<NewsCardDto> newsCards = [];
        IReadOnlyList<ReportCardDto> reportCards = [];
        IReadOnlyList<TrainingCardDto> trainingCards = [];
        IReadOnlyList<StatisticDto> stats = [];
        IReadOnlyList<ImportantLinkDto> links = [];

        // Sequential on purpose: a single DbContext does not support concurrent queries.
        if (CountFor(HomeSectionKey.VisionMission) is not null)
            visionMission = (await pages.GetSectionsAsync(SystemPages.VisionMission, lang, null, ct))
                .Where(s => s.Key is SectionKeys.Vision or SectionKeys.Mission).ToList();
        if (CountFor(HomeSectionKey.Responsibilities) is { } rc)
            responsibilities = (await pages.GetSectionsAsync(SystemPages.Responsibilities, lang, PageSectionLayout.Card, ct)).Take(rc).ToList();
        if (CountFor(HomeSectionKey.Atfm) is not null)
            atfmSteps = await pages.GetSectionsAsync(SystemPages.Atfm, lang, PageSectionLayout.ProcessStep, ct);
        if (CountFor(HomeSectionKey.Services) is { } sc) serviceCards = await services.GetAllAsync(lang, sc, ct);
        if (CountFor(HomeSectionKey.LatestNews) is { } nc) newsCards = await news.GetLatestAsync(lang, nc, ct);
        if (CountFor(HomeSectionKey.LatestReports) is { } pc) reportCards = await reports.GetLatestAsync(lang, pc, ct);
        if (CountFor(HomeSectionKey.Training) is { } tc) trainingCards = await training.GetUpcomingAsync(lang, tc, ct);
        if (CountFor(HomeSectionKey.Statistics) is not null)
            stats = await db.Statistics.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.DisplayOrder)
                .Select(s => new StatisticDto(lang == Lang.Ar ? s.LabelAr : s.LabelEn, s.Value, s.Icon)).ToListAsync(ct);
        if (CountFor(HomeSectionKey.ImportantLinks) is not null)
            links = await db.ImportantLinks.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.DisplayOrder)
                .Select(s => new ImportantLinkDto(lang == Lang.Ar ? s.TitleAr : s.TitleEn, s.Url, s.Icon)).ToListAsync(ct);

        return new HomePageDto(slides, blocks, visionMission, responsibilities, atfmSteps, serviceCards, newsCards, reportCards,
            trainingCards, stats, links);
    }

    private static HeroSlideDto ToSlide(HeroSlide s, Lang lang)
    {
        var title = lang.Pick(s.TitleAr, s.TitleEn);
        return new HeroSlideDto(title, lang.PickOrNull(s.SubtitleAr, s.SubtitleEn), lang.PickOrNull(s.DescriptionAr, s.DescriptionEn),
            ImageRef.From(s.Image, lang, title),
            Link(lang.PickOrNull(s.PrimaryButtonTextAr, s.PrimaryButtonTextEn), s.PrimaryButtonUrl),
            Link(lang.PickOrNull(s.SecondaryButtonTextAr, s.SecondaryButtonTextEn), s.SecondaryButtonUrl));
    }

    private static HomeBlockDto ToBlock(HomeSection s, Lang lang)
    {
        var title = lang.Pick(s.TitleAr, s.TitleEn);
        return new HomeBlockDto(s.Key, title, lang.PickOrNull(s.SubtitleAr, s.SubtitleEn), lang.PickOrNull(s.DescriptionAr, s.DescriptionEn),
            ImageRef.From(s.Image, lang, title), Link(lang.PickOrNull(s.ButtonTextAr, s.ButtonTextEn), s.ButtonUrl), s.ItemCount);
    }

    private static LinkDto? Link(string? text, string? url) =>
        string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(url) ? null : new LinkDto(text, url);
}
