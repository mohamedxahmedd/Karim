using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Web.Areas.Admin.ViewModels;

public sealed record StatusBadgeModel(ContentStatus Status, bool Scheduled = false);

public sealed record StatusActionsModel(Guid Id, ContentStatus Status);

public sealed record ListFilterModel(string? Search, ContentStatus? Status, bool ShowStatus = true, IReadOnlyList<SelectListItem>? Categories = null)
{
    public bool HasFilter => !string.IsNullOrEmpty(Search) || Status is not null || Categories?.Any(c => c.Selected) == true;
}

/// <summary>Generic admin list page: rows + filter state.</summary>
public sealed class AdminListModel<T>(PagedResult<T> items, ListFilterModel filter)
{
    public PagedResult<T> Items { get; } = items;
    public ListFilterModel Filter { get; } = filter;
}

/// <summary>Generic admin form: the application input plus lookup data for dropdowns.</summary>
public class AdminFormModel<TInput> where TInput : new()
{
    public TInput Input { get; set; } = new();
    [BindNever] public bool IsNew { get; set; }
    [BindNever] public IReadOnlyList<SelectListItem> Options { get; set; } = [];
    [BindNever] public IReadOnlyList<SelectListItem> SecondaryOptions { get; set; } = [];
}

public sealed class MediaEditModel(NationalAirspace.Application.Features.Media.MediaFileDto file, bool inUse)
{
    public NationalAirspace.Application.Features.Media.MediaFileDto File { get; } = file;
    public bool InUse { get; } = inUse;
    public NationalAirspace.Application.Features.Media.MediaMetadataInput Input { get; set; } = new();
}

public sealed record AlbumPhotosModel(
    NationalAirspace.Application.Features.MediaCenter.AlbumInput Album,
    IReadOnlyList<NationalAirspace.Application.Features.MediaCenter.AlbumPhotoAdminItem> Photos);

/// <summary>Icon picker shown under an icon text field (the field stays the source of truth and works without JavaScript).</summary>
public sealed record IconPickerModel(string? Value, string InputId = "Input_Icon");

/// <summary>Mini map of the demo grid: which cells hold which sector (the edited one is highlighted).</summary>
