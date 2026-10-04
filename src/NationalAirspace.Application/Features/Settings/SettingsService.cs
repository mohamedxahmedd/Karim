using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Entities;

namespace NationalAirspace.Application.Features.Settings;

public sealed class SettingsService(AdminServiceDependencies deps, ContentCache cache) : AdminServiceBase(deps)
{
    /// <summary>Hosts allowed in the contact-page map iframe (must match the CSP frame-src).</summary>
    public static readonly string[] AllowedMapHosts = ["www.google.com", "maps.google.com", "www.openstreetmap.org"];

    protected override string AuditEntityType => "Settings";

    public async Task<SettingsInput> GetForEditAsync(CancellationToken ct = default)
    {
        var s = await GetOrCreateAsync(ct);
        return new SettingsInput
        {
            ConcurrencyStamp = s.ConcurrencyStamp,
            SiteNameAr = s.SiteNameAr, SiteNameEn = s.SiteNameEn, TaglineAr = s.TaglineAr, TaglineEn = s.TaglineEn,
            AddressAr = s.AddressAr, AddressEn = s.AddressEn, Phone = s.Phone, Fax = s.Fax, Email = s.Email,
            WorkingHoursAr = s.WorkingHoursAr, WorkingHoursEn = s.WorkingHoursEn, MapEmbedUrl = s.MapEmbedUrl,
            Latitude = s.Latitude, Longitude = s.Longitude, FacebookUrl = s.FacebookUrl, XUrl = s.XUrl,
            LinkedInUrl = s.LinkedInUrl, YouTubeUrl = s.YouTubeUrl, InstagramUrl = s.InstagramUrl,
            FooterTextAr = s.FooterTextAr, FooterTextEn = s.FooterTextEn,
            DefaultMetaDescriptionAr = s.DefaultMetaDescriptionAr, DefaultMetaDescriptionEn = s.DefaultMetaDescriptionEn,
        };
    }

    public async Task<Result> UpdateAsync(SettingsInput input, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        if (!string.IsNullOrWhiteSpace(input.MapEmbedUrl)
            && (!Uri.TryCreate(input.MapEmbedUrl, UriKind.Absolute, out var map) || !AllowedMapHosts.Contains(map.Host)))
            errors.Add(new Error("Settings.MapHost", nameof(input.MapEmbedUrl)));
        if (errors.Count > 0) return Result.Failure(errors);

        var s = await GetOrCreateAsync(ct);
        ExpectStamp(s, input.ConcurrencyStamp);
        s.SiteNameAr = input.SiteNameAr.Trim();
        s.SiteNameEn = input.SiteNameEn.Trim();
        s.TaglineAr = Clean(input.TaglineAr);
        s.TaglineEn = Clean(input.TaglineEn);
        s.AddressAr = Clean(input.AddressAr);
        s.AddressEn = Clean(input.AddressEn);
        s.Phone = Clean(input.Phone);
        s.Fax = Clean(input.Fax);
        s.Email = Clean(input.Email);
        s.WorkingHoursAr = Clean(input.WorkingHoursAr);
        s.WorkingHoursEn = Clean(input.WorkingHoursEn);
        s.MapEmbedUrl = Clean(input.MapEmbedUrl);
        s.Latitude = input.Latitude;
        s.Longitude = input.Longitude;
        s.FacebookUrl = Clean(input.FacebookUrl);
        s.XUrl = Clean(input.XUrl);
        s.LinkedInUrl = Clean(input.LinkedInUrl);
        s.YouTubeUrl = Clean(input.YouTubeUrl);
        s.InstagramUrl = Clean(input.InstagramUrl);
        s.FooterTextAr = Clean(input.FooterTextAr);
        s.FooterTextEn = Clean(input.FooterTextEn);
        s.DefaultMetaDescriptionAr = Clean(input.DefaultMetaDescriptionAr);
        s.DefaultMetaDescriptionEn = Clean(input.DefaultMetaDescriptionEn);
        Audit("Update", s.Id, null);
        return await CommitAsync(ct);
    }

    public Task<SiteInfoDto> GetSiteInfoAsync(Lang lang, CancellationToken ct = default) =>
        cache.GetOrCreateAsync($"site-info:{lang}", async () =>
        {
            var s = await Db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct) ?? new SiteSettings();
            var social = new List<SocialLink>();
            void Add(string network, string? url, string icon) { if (!string.IsNullOrWhiteSpace(url)) social.Add(new SocialLink(network, url, icon)); }
            Add("Facebook", s.FacebookUrl, "bi-facebook");
            Add("X", s.XUrl, "bi-twitter-x");
            Add("LinkedIn", s.LinkedInUrl, "bi-linkedin");
            Add("YouTube", s.YouTubeUrl, "bi-youtube");
            Add("Instagram", s.InstagramUrl, "bi-instagram");
            return new SiteInfoDto(
                lang.Pick(s.SiteNameAr, s.SiteNameEn), lang.PickOrNull(s.TaglineAr, s.TaglineEn), lang.PickOrNull(s.AddressAr, s.AddressEn),
                s.Phone, s.Fax, s.Email, lang.PickOrNull(s.WorkingHoursAr, s.WorkingHoursEn), s.MapEmbedUrl, s.Latitude, s.Longitude,
                social, lang.PickOrNull(s.FooterTextAr, s.FooterTextEn), lang.PickOrNull(s.DefaultMetaDescriptionAr, s.DefaultMetaDescriptionEn));
        });

    private async Task<SiteSettings> GetOrCreateAsync(CancellationToken ct)
    {
        var s = await Db.SiteSettings.FirstOrDefaultAsync(x => x.Id == SiteSettings.SingletonId, ct);
        if (s is not null) return s;
        s = new SiteSettings { Id = SiteSettings.SingletonId, SiteNameAr = "المركز القومي لإدارة المجال الجوي", SiteNameEn = "National Airspace Management Center" };
        Db.SiteSettings.Add(s);
        await Db.SaveChangesAsync(ct);
        return s;
    }
}
