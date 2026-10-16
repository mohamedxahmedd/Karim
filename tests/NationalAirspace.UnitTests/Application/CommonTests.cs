using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Contact;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Domain.Enums;
using NationalAirspace.Infrastructure.Services;
using NationalAirspace.UnitTests.Support;

namespace NationalAirspace.UnitTests.Application;

public class SlugGeneratorTests
{
    [Fact]
    public void English_title_becomes_lowercase_hyphenated() =>
        Assert.Equal("air-traffic-flow-management", SlugGenerator.Generate("Air Traffic Flow Management!"));

    [Fact]
    public void Arabic_letters_are_kept() =>
        Assert.Equal("إدارة-المجال-الجوي", SlugGenerator.Generate("إدارة المجال الجوي"));

    [Fact]
    public void Punctuation_only_gives_empty_slug() => Assert.Equal(string.Empty, SlugGenerator.Generate("!!! ???"));

    [Fact]
    public async Task Taken_slug_gets_numeric_suffix()
    {
        var taken = new HashSet<string> { "news", "news-2" };
        Assert.Equal("news-3", await SlugGenerator.EnsureUniqueAsync("news", s => Task.FromResult(taken.Contains(s))));
    }
}

public class LangTests
{
    [Fact]
    public void Pick_returns_requested_language() => Assert.Equal("عربي", Lang.Ar.Pick("عربي", "English"));

    [Fact]
    public void Pick_falls_back_to_other_language_when_empty() => Assert.Equal("English", Lang.Ar.Pick("  ", "English"));

    [Fact]
    public void PickOrNull_returns_null_when_both_empty() => Assert.Null(Lang.En.PickOrNull(null, ""));
}

public class FileSignatureTests
{
    [Fact]
    public void Png_header_matches_png_extension() => Assert.True(FileSignatures.Matches(".png", SampleBytes.PngHeader));

    [Fact]
    public void Executable_renamed_to_png_is_rejected() => Assert.False(FileSignatures.Matches(".png", SampleBytes.WindowsExe));

    [Fact]
    public void Unknown_extension_is_not_allowed() => Assert.False(FileSignatures.TryGetType(".exe", out _, out _));
}

public class HtmlSanitizerTests
{
    private readonly HtmlSanitizerService _sanitizer = new();

    [Fact]
    public void Scripts_and_event_handlers_are_removed()
    {
        var html = _sanitizer.Sanitize("<p onclick=\"steal()\">Hi<script>alert(1)</script><img src=x onerror=alert(2)></p>")!;
        Assert.DoesNotContain("script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Basic_formatting_is_kept() => Assert.Contains("<strong>bold</strong>", _sanitizer.Sanitize("<p><strong>bold</strong></p>"));

    [Fact]
    public void Javascript_links_are_removed() =>
        Assert.DoesNotContain("javascript:", _sanitizer.Sanitize("<a href=\"javascript:alert(1)\">x</a>")!, StringComparison.OrdinalIgnoreCase);
}

public class ContactSpamCheckTests
{
    private static readonly DateTime Now = new(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Filled_honeypot_is_a_bot() => Assert.True(ContactSpamCheck.IsLikelyBot("http://spam", Now.AddMinutes(-1), Now));

    [Fact]
    public void Submission_faster_than_minimum_fill_time_is_a_bot() => Assert.True(ContactSpamCheck.IsLikelyBot(null, Now.AddSeconds(-1), Now));

    [Fact]
    public void Missing_or_expired_form_token_is_a_bot()
    {
        Assert.True(ContactSpamCheck.IsLikelyBot(null, null, Now));
        Assert.True(ContactSpamCheck.IsLikelyBot(null, Now.AddHours(-7), Now));
    }

    [Fact]
    public void Normal_submission_is_accepted() => Assert.False(ContactSpamCheck.IsLikelyBot("", Now.AddSeconds(-40), Now));
}
