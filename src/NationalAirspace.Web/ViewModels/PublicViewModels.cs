using Microsoft.AspNetCore.Mvc.ModelBinding;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Contact;
using NationalAirspace.Application.Features.MediaCenter;
using NationalAirspace.Application.Features.News;
using NationalAirspace.Application.Features.Reports;
using NationalAirspace.Application.Features.Search;
using NationalAirspace.Application.Features.Settings;

namespace NationalAirspace.Web.ViewModels;

public class ListPageModel<T>(PagedResult<T> items, string? query)
{
    public PagedResult<T> Items { get; } = items;
    public string? Query { get; } = query;
    public string? Category { get; init; }
    public string? Tab { get; init; }
}

public sealed class NewsListModel(PagedResult<NewsCardDto> items, string? query, string? category, IReadOnlyList<NewsCategoryDto> categories)
    : ListPageModel<NewsCardDto>(items, query)
{
    public new string? Category { get; } = category;
    public IReadOnlyList<NewsCategoryDto> Categories { get; } = categories;
}

public sealed class ReportListModel(PagedResult<ReportCardDto> items, string? query, string? category, int? year,
    IReadOnlyList<NewsCategoryDto> categories, IReadOnlyList<int> years) : ListPageModel<ReportCardDto>(items, query)
{
    public new string? Category { get; } = category;
    public int? Year { get; } = year;
    public IReadOnlyList<NewsCategoryDto> Categories { get; } = categories;
    public IReadOnlyList<int> Years { get; } = years;
}

public sealed record MediaCenterModel(PagedResult<AlbumCardDto> Albums, PagedResult<VideoDto> Videos);

public sealed record SearchModel(string? Query, IReadOnlyList<SearchHit> Hits);

public sealed class ContactPageModel
{
    public ContactInput Input { get; set; } = new();
    /// <summary>Signed render timestamp (bot mitigation).</summary>
    public string? FormToken { get; set; }
    /// <summary>Honeypot: must stay empty.</summary>
    public string? Website { get; set; }
    [BindNever] public bool Sent { get; set; }
    [BindNever] public SiteInfoDto? Site { get; set; }
}
