using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Features.Contact;
using NationalAirspace.Application.Features.Settings;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Localization;
using NationalAirspace.Web.Services;
using NationalAirspace.Web.ViewModels;

namespace NationalAirspace.Web.Controllers;

/// <summary>Contact page and form. Not output-cached (antiforgery + per-request timestamp token).</summary>
[Route("{culture:culture}/contact")]
public sealed class ContactController(
    ContactService contact,
    SettingsService settings,
    FormTimestampProtector timestamps,
    IpHasher ipHasher,
    IClock clock,
    IStringLocalizer<SharedResource> L) : PublicController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(bool sent = false, CancellationToken ct = default)
    {
        return View(await BuildAsync(new ContactInput(), sent, ct));
    }

    [HttpPost("")]
    [EnableRateLimiting(ServiceCollectionExtensions.ContactRateLimit)]
    public async Task<IActionResult> Index(ContactPageModel model, CancellationToken ct)
    {
        // Bot mitigation: hidden honeypot + signed minimum fill time. Bots get a generic success response.
        if (ContactSpamCheck.IsLikelyBot(model.Website, timestamps.Read(model.FormToken), clock.UtcNow))
            return Redirect(Url.Site("/contact") + "?sent=true");

        if (ModelState.IsValid)
        {
            var result = await contact.SubmitAsync(model.Input, ipHasher.Hash(HttpContext.Connection.RemoteIpAddress?.ToString()), CultureHelper.CurrentCode, ct);
            if (result.Succeeded) return Redirect(Url.Site("/contact") + "?sent=true");
            ModelState.AddResultErrors(result, L);
        }
        return View(await BuildAsync(model.Input, false, ct));
    }

    private async Task<ContactPageModel> BuildAsync(ContactInput input, bool sent, CancellationToken ct)
    {
        Seo(L["Nav.Contact"], L["Contact.Lead"]);
        ViewData["Lead"] = L["Contact.Lead"].Value;
        Breadcrumb((L["Nav.Contact"], null));
        return new ContactPageModel
        {
            Input = input,
            FormToken = timestamps.Issue(),
            Sent = sent,
            Site = await settings.GetSiteInfoAsync(Lang, ct),
        };
    }
}
