using Microsoft.Extensions.Caching.Memory;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Settings;
using NationalAirspace.Domain.Enums;
using NationalAirspace.UnitTests.Support;

namespace NationalAirspace.UnitTests.Application;

public sealed class FooterSettingsTests : IDisposable
{
    private readonly TestHost _host = new();

    private SettingsService Service() => new(_host.Deps(_host.NewContext()), new ContentCache(new MemoryCache(new MemoryCacheOptions())));

    [Fact]
    public async Task Update_saves_trimmed_texts_and_clears_empty_ones()
    {
        var input = await Service().GetFooterForEditAsync();
        input.FooterLinksTitleAr = "  روابط مهمة ";
        input.FooterCopyrightEn = "   ";

        Assert.True((await Service().UpdateFooterAsync(input)).Succeeded);

        var saved = await Service().GetFooterForEditAsync();
        Assert.Equal("روابط مهمة", saved.FooterLinksTitleAr);
        Assert.Null(saved.FooterCopyrightEn);
    }

    [Fact]
    public async Task Update_evicts_public_cache()
    {
        var input = await Service().GetFooterForEditAsync();
        await Service().UpdateFooterAsync(input);
        Assert.Equal(1, _host.Cache.Calls);
    }

    [Fact]
    public async Task Stale_stamp_returns_concurrency_error()
    {
        var first = await Service().GetFooterForEditAsync();
        var second = await Service().GetFooterForEditAsync();
        first.FooterTextEn = "Edited by someone else";
        await Service().UpdateFooterAsync(first);

        var result = await Service().UpdateFooterAsync(second);

        Assert.Contains(Error.Concurrency, result.Errors);
    }

    [Fact]
    public async Task Saving_general_settings_keeps_footer_text()
    {
        var footer = await Service().GetFooterForEditAsync();
        footer.FooterTextAr = "نبذة";
        await Service().UpdateFooterAsync(footer);

        Assert.True((await Service().UpdateAsync(await Service().GetForEditAsync())).Succeeded);

        Assert.Equal("نبذة", (await Service().GetFooterForEditAsync()).FooterTextAr);
    }

    [Fact]
    public async Task Heading_set_in_one_language_keeps_default_in_the_other()
    {
        var input = await Service().GetFooterForEditAsync();
        input.FooterContactTitleAr = "تواصل";
        await Service().UpdateFooterAsync(input);

        Assert.Equal("تواصل", (await Service().GetSiteInfoAsync(Lang.Ar)).Footer.ContactTitle);
        Assert.Null((await Service().GetSiteInfoAsync(Lang.En)).Footer.ContactTitle);
    }

    public void Dispose() => _host.Dispose();
}
