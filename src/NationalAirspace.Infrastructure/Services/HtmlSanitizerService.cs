using Ganss.Xss;
using IHtmlSanitizer = NationalAirspace.Application.Abstractions.IHtmlSanitizer;

namespace NationalAirspace.Infrastructure.Services;

/// <summary>Allow-list based HTML sanitizer for rich text produced by the admin editor.</summary>
public sealed class HtmlSanitizerService : IHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedAttributes.Add("dir");
        _sanitizer.AllowedAttributes.Add("target");
        _sanitizer.AllowedSchemes.Add("mailto");
        _sanitizer.AllowedSchemes.Add("tel");
        _sanitizer.AllowedTags.Remove("form");
        _sanitizer.AllowedTags.Remove("input");
        _sanitizer.AllowedTags.Remove("button");
        _sanitizer.AllowedTags.Remove("select");
        _sanitizer.AllowedTags.Remove("textarea");
        _sanitizer.PostProcessNode += (_, e) =>
        {
            if (e.Node is AngleSharp.Dom.IElement { TagName: "A" } a && a.GetAttribute("target") is { } target)
            {
                if (target != "_blank") a.RemoveAttribute("target");
                else a.SetAttribute("rel", "noopener noreferrer");
            }
        };
    }

    public string? Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var clean = _sanitizer.Sanitize(html).Trim();
        // Quill emits "<p><br></p>" for an empty editor.
        return clean is "" or "<p><br></p>" ? null : clean;
    }
}
