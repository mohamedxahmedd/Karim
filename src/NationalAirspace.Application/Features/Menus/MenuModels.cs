using System.ComponentModel.DataAnnotations;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Menus;

public class MenuItemInput
{
    public Guid? Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public MenuLocation Location { get; set; }
    public Guid? ParentId { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string TitleAr { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string TitleEn { get; set; } = string.Empty;
    /// <summary>"/about" (culture is added automatically) or an absolute http(s) URL.</summary>
    [Required(ErrorMessage = V.Required), StringLength(500, ErrorMessage = V.MaxLength), SafeLink] public string Url { get; set; } = "/";
    public bool OpenInNewTab { get; set; }
    public bool IsVisible { get; set; } = true;
}

public sealed record MenuAdminItem(Guid Id, Guid? ParentId, string TitleAr, string TitleEn, string Url, bool IsVisible, bool OpenInNewTab, int DisplayOrder, IReadOnlyList<MenuAdminItem> Children);

/// <summary>Localized menu node for rendering.</summary>
public sealed record MenuNode(string Title, string Url, bool IsExternal, bool OpenInNewTab, IReadOnlyList<MenuNode> Children);
