using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;
using NationalAirspace.Web.Localization;

namespace NationalAirspace.Web.TagHelpers;

/// <summary>
/// Renders a complete Bootstrap form group: localized label, input, hint and validation message.
/// Text direction is inferred from the property suffix (…Ar → rtl, …En → ltr).
/// <code>&lt;field asp-for="Input.TitleAr" /&gt;  &lt;field asp-for="Input.ContentAr" type="richtext" /&gt;</code>
/// </summary>
[HtmlTargetElement("field", Attributes = "asp-for", TagStructure = TagStructure.WithoutEndTag)]
public sealed class FormFieldTagHelper(IHtmlGenerator generator, IStringLocalizer<SharedResource> localizer) : TagHelper
{
    [HtmlAttributeName("asp-for")] public ModelExpression For { get; set; } = default!;
    /// <summary>text | textarea | richtext | date | datetime | number | checkbox | select | email | url | tel | password</summary>
    [HtmlAttributeName("type")] public string Type { get; set; } = "text";
    [HtmlAttributeName("rows")] public int Rows { get; set; } = 4;
    [HtmlAttributeName("items")] public IEnumerable<SelectListItem>? Items { get; set; }
    /// <summary>Resource key of the empty option for selects.</summary>
    [HtmlAttributeName("empty")] public string? Empty { get; set; }
    /// <summary>Resource key of a help text shown under the input.</summary>
    [HtmlAttributeName("hint")] public string? Hint { get; set; }
    /// <summary>Resource key overriding the label.</summary>
    [HtmlAttributeName("label")] public string? Label { get; set; }
    [HtmlAttributeName("placeholder")] public string? Placeholder { get; set; }
    [HtmlAttributeName("dir")] public string? Dir { get; set; }
    [HtmlAttributeName("readonly")] public bool ReadOnly { get; set; }
    [HtmlAttributeName("class")] public string? CssClass { get; set; }

    [ViewContext, HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var name = For.Name;
        var property = For.Metadata.PropertyName ?? name;
        var type = Type.ToLowerInvariant();
        var hasError = ViewContext.ViewData.ModelState.TryGetValue(name, out var entry) && entry.Errors.Count > 0;

        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", (type == "checkbox" ? "form-check form-switch mb-3 " : "mb-3 ") + CssClass);

        var dir = Dir ?? (property.EndsWith("Ar", StringComparison.Ordinal) ? "rtl" : property.EndsWith("En", StringComparison.Ordinal) ? "ltr" : null);
        var attrs = new Dictionary<string, object> { ["class"] = (type == "select" ? "form-select" : type == "checkbox" ? "form-check-input" : "form-control") + (hasError ? " is-invalid" : "") };
        if (dir is not null) attrs["dir"] = dir;
        if (dir is not null) attrs["lang"] = dir == "rtl" ? "ar" : "en";
        if (!string.IsNullOrEmpty(Placeholder)) attrs["placeholder"] = localizer[Placeholder].Value;
        if (ReadOnly) attrs["readonly"] = "readonly";
        if (type == "checkbox") attrs["role"] = "switch";

        var labelText = Label is not null ? localizer[Label].Value : For.Metadata.GetDisplayName();
        var required = For.Metadata.IsRequired && type is not ("checkbox" or "select") && !For.Metadata.ModelType.IsValueType;
        var label = generator.GenerateLabel(ViewContext, For.ModelExplorer, name, labelText, new { @class = type == "checkbox" ? "form-check-label" : "form-label" });
        if (required) label.InnerHtml.AppendHtml(" <span class=\"text-danger\" aria-hidden=\"true\">*</span>");

        TagBuilder input = type switch
        {
            "textarea" => generator.GenerateTextArea(ViewContext, For.ModelExplorer, name, Rows, 0, attrs),
            "richtext" => RichText(name, attrs),
            "select" => generator.GenerateSelect(ViewContext, For.ModelExplorer, Empty is null ? null : localizer[Empty].Value, name, Items ?? [], false, attrs),
            "checkbox" => generator.GenerateCheckBox(ViewContext, For.ModelExplorer, name, null, attrs),
            "password" => generator.GeneratePassword(ViewContext, For.ModelExplorer, name, null, Merge(attrs, "autocomplete", "new-password")),
            "date" => TextBox(name, "{0:yyyy-MM-dd}", Merge(attrs, "type", "date")),
            "datetime" => TextBox(name, "{0:yyyy-MM-ddTHH:mm}", Merge(attrs, "type", "datetime-local")),
            "number" or "email" or "url" or "tel" => TextBox(name, null, Merge(attrs, "type", type)),
            _ => TextBox(name, null, attrs),
        };

        if (type == "checkbox")
        {
            output.Content.AppendHtml(input);
            output.Content.AppendHtml(generator.GenerateHiddenForCheckbox(ViewContext, For.ModelExplorer, name));
            output.Content.AppendHtml(label);
        }
        else
        {
            output.Content.AppendHtml(label);
            output.Content.AppendHtml(input);
        }

        if (!string.IsNullOrEmpty(Hint))
            output.Content.AppendHtml($"<div class=\"form-text\">{System.Net.WebUtility.HtmlEncode(localizer[Hint].Value)}</div>");
        var validation = generator.GenerateValidationMessage(ViewContext, For.ModelExplorer, name, null, "div", new { @class = "invalid-feedback d-block" });
        if (validation is not null) output.Content.AppendHtml(validation);
    }

    private TagBuilder TextBox(string name, string? format, IDictionary<string, object> attrs) =>
        generator.GenerateTextBox(ViewContext, For.ModelExplorer, name, For.Model, format, attrs);

    private TagBuilder RichText(string name, IDictionary<string, object> attrs)
    {
        attrs["class"] += " richtext-source";
        attrs["data-richtext"] = "true";
        return generator.GenerateTextArea(ViewContext, For.ModelExplorer, name, Math.Max(Rows, 10), 0, attrs);
    }

    private static Dictionary<string, object> Merge(IDictionary<string, object> attrs, string key, string value) =>
        new(attrs) { [key] = value };
}
