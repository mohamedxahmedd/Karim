using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Menus;

/// <summary>Header/footer navigation management (two levels: item → children).</summary>
public sealed class MenuService(AdminServiceDependencies deps, ContentCache cache) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "MenuItem";

    public async Task<IReadOnlyList<MenuAdminItem>> GetTreeAsync(MenuLocation location, CancellationToken ct = default)
    {
        var items = await Db.MenuItems.AsNoTracking().Where(m => m.Location == location).OrderBy(m => m.DisplayOrder).ToListAsync(ct);
        IReadOnlyList<MenuAdminItem> Build(Guid? parent) => items.Where(i => i.ParentId == parent)
            .Select(i => new MenuAdminItem(i.Id, i.ParentId, i.TitleAr, i.TitleEn, i.Url, i.IsVisible, i.OpenInNewTab, i.DisplayOrder,
                parent is null ? Build(i.Id) : [])).ToList();
        return Build(null);
    }

    public async Task<IReadOnlyList<MenuAdminItem>> GetParentOptionsAsync(MenuLocation location, Guid? excludeId, CancellationToken ct = default) =>
        await Db.MenuItems.AsNoTracking().Where(m => m.Location == location && m.ParentId == null && m.Id != excludeId)
            .OrderBy(m => m.DisplayOrder)
            .Select(i => new MenuAdminItem(i.Id, null, i.TitleAr, i.TitleEn, i.Url, i.IsVisible, i.OpenInNewTab, i.DisplayOrder, new List<MenuAdminItem>()))
            .ToListAsync(ct);

    public async Task<MenuItemInput?> GetForEditAsync(Guid id, CancellationToken ct = default) =>
        await Db.MenuItems.AsNoTracking().Where(m => m.Id == id).Select(m => new MenuItemInput
        {
            Id = m.Id, ConcurrencyStamp = m.ConcurrencyStamp, Location = m.Location, ParentId = m.ParentId,
            TitleAr = m.TitleAr, TitleEn = m.TitleEn, Url = m.Url, OpenInNewTab = m.OpenInNewTab, IsVisible = m.IsVisible,
        }).FirstOrDefaultAsync(ct);

    public async Task<Result<Guid>> SaveAsync(MenuItemInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        var entity = input.Id is { } id ? await Db.MenuItems.FirstOrDefaultAsync(m => m.Id == id, ct) : new MenuItem();
        if (entity is null) return Result<Guid>.Failure(Error.NotFound);

        if (input.ParentId is { } parentId)
        {
            var parent = await Db.MenuItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == parentId, ct);
            var hasChildren = input.Id is not null && await Db.MenuItems.AnyAsync(m => m.ParentId == entity.Id, ct);
            if (parent is null || parent.Id == entity.Id || parent.ParentId is not null || parent.Location != input.Location || hasChildren)
                errors.Add(new Error("Menus.InvalidParent", nameof(input.ParentId)));
        }
        if (errors.Count > 0) return Result<Guid>.Failure(errors);

        var siblingsChanged = input.Id is null || entity.ParentId != input.ParentId || entity.Location != input.Location;
        entity.Location = input.Location;
        entity.ParentId = input.ParentId;
        entity.TitleAr = input.TitleAr.Trim();
        entity.TitleEn = input.TitleEn.Trim();
        entity.Url = input.Url.Trim();
        entity.OpenInNewTab = input.OpenInNewTab;
        entity.IsVisible = input.IsVisible;
        if (siblingsChanged)
            entity.DisplayOrder = await NextOrderAsync(Db.MenuItems.Where(m => m.Location == input.Location && m.ParentId == input.ParentId), ct);

        if (input.Id is null)
        {
            Db.MenuItems.Add(entity);
            Audit("Create", entity.Id, entity.TitleAr);
        }
        else
        {
            ExpectStamp(entity, input.ConcurrencyStamp);
            Audit("Update", entity.Id, entity.TitleAr);
        }
        return await CommitAsync(entity.Id, ct);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await Db.MenuItems.Include(m => m.Children).FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity is null) return Result.Failure(Error.NotFound);
        foreach (var child in entity.Children.ToList()) Db.MenuItems.Remove(child);
        Db.MenuItems.Remove(entity);
        Audit("Delete", id, entity.TitleAr);
        return await CommitAsync(ct);
    }

    public async Task<Result> MoveAsync(Guid id, int direction, CancellationToken ct = default)
    {
        var item = await Db.MenuItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
        if (item is null) return Result.Failure(Error.NotFound);
        return await MoveAsync(Db.MenuItems.Where(m => m.Location == item.Location && m.ParentId == item.ParentId), id, direction, ct);
    }

    public async Task<Result> ToggleVisibleAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await Db.MenuItems.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity is null) return Result.Failure(Error.NotFound);
        entity.IsVisible = !entity.IsVisible;
        Audit(entity.IsVisible ? "Show" : "Hide", id, entity.TitleAr);
        return await CommitAsync(ct);
    }

    /// <summary>Visible, localized menu tree (cached). URLs are returned as stored; the web layer prefixes the culture.</summary>
    public Task<IReadOnlyList<MenuNode>> GetMenuAsync(MenuLocation location, Lang lang, CancellationToken ct = default) =>
        cache.GetOrCreateAsync($"menu:{location}:{lang}", async () =>
        {
            var items = await Db.MenuItems.AsNoTracking().Where(m => m.Location == location && m.IsVisible)
                .OrderBy(m => m.DisplayOrder).ToListAsync(ct);
            IReadOnlyList<MenuNode> Build(Guid? parent) => items.Where(i => i.ParentId == parent)
                .Select(i => new MenuNode(lang.Pick(i.TitleAr, i.TitleEn), i.Url, i.IsExternal, i.OpenInNewTab, parent is null ? Build(i.Id) : []))
                .ToList();
            return Build(null);
        });
}
