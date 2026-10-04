using System.Net;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Domain.Enums;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Localization;

namespace NationalAirspace.Web.TagHelpers;

/// <summary>
/// Media reference editor: shows the current file, lets the editor upload a new one (works without JS)
/// or pick/clear one from the library (progressive enhancement).
/// <code>&lt;media-field asp-for="Input.CoverImageId" kind="Image" upload="CoverImageUpload" /&gt;</code>
/// </summary>
[HtmlTargetElement("media-field", Attributes = "asp-for", TagStructure = TagStructure.WithoutEndTag)]
public sealed class MediaFieldTagHelper(MediaService media, IOptions<MediaOptions> options, IHtmlGenerator generator, IStringLocalizer<SharedResource> L) : TagHelper
{
    [HtmlAttributeName("asp-for")] public ModelExpression For { get; set; } = default!;
    [HtmlAttributeName("kind")] public MediaKind Kind { get; set; } = MediaKind.Image;
    /// <summary>Name of the file input (bound by the controller).</summary>
    [HtmlAttributeName("upload")] public string Upload { get; set; } = default!;
    [HtmlAttributeName("label")] public string? Label { get; set; }

    [ViewContext, HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = default!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var name = For.Name;
        var attempted = ViewContext.ViewData.ModelState.TryGetValue(name, out var entry) ? entry.AttemptedValue : null;
        Guid? id = Guid.TryParse(attempted, out var parsed) ? parsed : For.Model as Guid?;
        var current = id is { } value ? await media.GetAsync(value) : null;
        var id_ = TagBuilder.CreateSanitizedId(name, "_");
        string E(string s) => WebUtility.HtmlEncode(s);

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "mb-3 media-field");
        output.Attributes.SetAttribute("data-media-field", Kind.ToString());

        var label = Label is not null ? L[Label].Value : For.Metadata.GetDisplayName();
        var preview = current switch
        {
            { Url: { } url } => $"<img src=\"{E(url)}\" alt=\"\" class=\"media-thumb\">",
            not null => $"<span class=\"media-doc\"><i class=\"bi bi-file-earmark-text\"></i> {E(current.OriginalFileName)} <small class=\"text-muted\">({current.SizeBytes.FileSize()})</small></span>",
            _ => $"<span class=\"text-muted small\">{E(L["Media.NoFile"].Value)}</span>",
        };
        var max = options.Value.MaxBytesFor(Kind) / (1024 * 1024);
        var hint = string.Format(System.Globalization.CultureInfo.CurrentCulture, L["Media.UploadHint"].Value, FileSignatures.AcceptAttribute(Kind), max);

        output.Content.AppendHtml($"""
            <label class="form-label" for="{E(Upload)}">{E(label)}</label>
            <div class="media-field-box">
              <div class="media-preview" data-media-preview>{preview}</div>
              <div class="media-actions">
                <input type="file" id="{E(Upload)}" name="{E(Upload)}" accept="{E(FileSignatures.AcceptAttribute(Kind))}" class="form-control form-control-sm">
                <div class="d-flex gap-2">
                  <button type="button" class="btn btn-sm btn-outline-primary" data-media-pick><i class="bi bi-images"></i> {E(L["Media.ChooseFromLibrary"].Value)}</button>
                  <button type="button" class="btn btn-sm btn-outline-danger" data-media-clear><i class="bi bi-x-lg"></i> {E(L["Media.Remove"].Value)}</button>
                </div>
              </div>
              <input type="hidden" id="{id_}" name="{E(name)}" value="{id}" data-media-value>
              <div class="form-text">{E(hint)}</div>
            </div>
            """);
        var validation = generator.GenerateValidationMessage(ViewContext, For.ModelExplorer, name, null, "div", new { @class = "invalid-feedback d-block" });
        if (validation is not null) output.Content.AppendHtml(validation);
    }
}
