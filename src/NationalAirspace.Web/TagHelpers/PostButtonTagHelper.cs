using System.Net;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;
using NationalAirspace.Web.Localization;

namespace NationalAirspace.Web.TagHelpers;

/// <summary>
/// A button that submits a small antiforgery-protected POST form (state changes never use GET).
/// <code>&lt;post-button asp-action="Delete" asp-route-id="@x.Id" text="Common.Delete" icon="bi-trash" confirm="Admin.ConfirmDelete" /&gt;</code>
/// </summary>
[HtmlTargetElement("post-button", Attributes = "asp-action", TagStructure = TagStructure.WithoutEndTag)]
public sealed class PostButtonTagHelper(IUrlHelperFactory urlFactory, IAntiforgery antiforgery, IStringLocalizer<SharedResource> L) : TagHelper
{
    [HtmlAttributeName("asp-action")] public string Action { get; set; } = default!;
    [HtmlAttributeName("asp-controller")] public string? Controller { get; set; }
    [HtmlAttributeName("asp-all-route-data", DictionaryAttributePrefix = "asp-route-")]
    public IDictionary<string, string?> RouteValues { get; set; } = new Dictionary<string, string?>();
    /// <summary>Resource key of the button text.</summary>
    [HtmlAttributeName("text")] public string? Text { get; set; }
    [HtmlAttributeName("icon")] public string? Icon { get; set; }
    [HtmlAttributeName("class")] public string CssClass { get; set; } = "btn btn-sm btn-outline-secondary";
    /// <summary>Resource key of a confirmation question (shown in a modal).</summary>
    [HtmlAttributeName("confirm")] public string? Confirm { get; set; }
    [HtmlAttributeName("title")] public string? Title { get; set; }
    /// <summary>When true (default) the current URL is posted as returnUrl so the list keeps its filters/page.</summary>
    [HtmlAttributeName("return-here")] public bool ReturnHere { get; set; } = true;

    [ViewContext, HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        string E(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);
        var url = urlFactory.GetUrlHelper(ViewContext);
        var values = new Dictionary<string, object?>(RouteValues.ToDictionary(kv => kv.Key, kv => (object?)kv.Value));
        var action = url.Action(Action, Controller, values) ?? "#";
        var tokens = antiforgery.GetAndStoreTokens(ViewContext.HttpContext);
        var request = ViewContext.HttpContext.Request;
        var text = Text is null ? null : L[Text].Value;
        var title = Title is null ? text : L[Title].Value;

        output.TagName = "form";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("method", "post");
        output.Attributes.SetAttribute("action", action);
        output.Attributes.SetAttribute("class", "d-inline");
        if (Confirm is not null) output.Attributes.SetAttribute("data-confirm", L[Confirm].Value);

        var html = $"<input type=\"hidden\" name=\"{E(tokens.FormFieldName)}\" value=\"{E(tokens.RequestToken)}\">";
        if (ReturnHere) html += $"<input type=\"hidden\" name=\"returnUrl\" value=\"{E(request.PathBase + request.Path + request.QueryString)}\">";
        var icon = Icon is null ? string.Empty : $"<i class=\"bi {E(Icon)}\" aria-hidden=\"true\"></i>";
        var label = text is null ? string.Empty : (Icon is null ? E(text) : $" <span>{E(text)}</span>");
        var aria = text is null && title is not null ? $" aria-label=\"{E(title)}\" title=\"{E(title)}\"" : string.Empty;
        html += $"<button type=\"submit\" class=\"{E(CssClass)}\"{aria}>{icon}{label}</button>";
        output.Content.SetHtmlContent(html);
    }
}
