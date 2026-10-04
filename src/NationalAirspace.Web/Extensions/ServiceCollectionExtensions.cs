using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common.Security;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Infrastructure.Storage;
using NationalAirspace.Web.Localization;
using NationalAirspace.Web.Services;

namespace NationalAirspace.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public const string PublicCachePolicy = "Public";
    public const string ContactRateLimit = "contact";
    public const string LoginRateLimit = "login";

    public static IServiceCollection AddWeb(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        // ---- localization
        services.AddLocalization(o => o.ResourcesPath = "Resources");
        services.Configure<RequestLocalizationOptions>(o =>
        {
            o.DefaultRequestCulture = new RequestCulture(CultureHelper.ArabicCulture);
            o.SupportedCultures = CultureHelper.SupportedCultures;
            o.SupportedUICultures = CultureHelper.SupportedCultures;
            o.FallBackToParentCultures = true;
            o.FallBackToParentUICultures = true;
            o.RequestCultureProviders = [new RouteCultureProvider(), new CookieRequestCultureProvider()];
        });
        services.Configure<RouteOptions>(o =>
        {
            o.ConstraintMap["culture"] = typeof(CultureRouteConstraint);
            o.LowercaseUrls = false;
        });

        // ---- MVC
        services.AddTransient<IConfigureOptions<MvcOptions>, LocalizedMetadataSetup>();
        services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization(o => o.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));
        services.AddAntiforgery(o =>
        {
            o.Cookie.Name = "namc.af";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Strict;
            o.Cookie.SecurePolicy = env.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });

        // ---- authentication cookie & authorization policies
        services.ConfigureApplicationCookie(o =>
        {
            o.Cookie.Name = "namc.auth";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = env.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            o.ExpireTimeSpan = TimeSpan.FromHours(2);
            o.SlidingExpiration = true;
            o.LoginPath = "/Admin/Account/Login";
            o.LogoutPath = "/Admin/Account/Logout";
            o.AccessDeniedPath = "/Admin/Account/AccessDenied";
            o.ReturnUrlParameter = "returnUrl";
        });
        services.AddAuthorization(o =>
        {
            foreach (var permission in Permissions.All)
                o.AddPolicy(permission, p => p.RequireAuthenticatedUser().RequireRole(Permissions.RolesGranting(permission)));
        });

        // ---- data protection (persisted so cookies survive restarts / multiple instances share keys)
        var keysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(keysPath)) keysPath = Path.Combine(env.ContentRootPath, "App_Data", "keys");
        services.AddDataProtection().SetApplicationName("NationalAirspaceCms").PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        // ---- caching & rate limiting
        services.AddOutputCache(o =>
        {
            o.AddPolicy(PublicCachePolicy, b => b.Expire(TimeSpan.FromSeconds(60)).SetVaryByQuery("*").Tag(ContentCacheInvalidator.Tag));
        });
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(ContactRateLimit, ctx => RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
            o.AddPolicy(LoginRateLimit, ctx => RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });

        // ---- reverse proxy
        services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var proxy in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
                if (IPAddress.TryParse(proxy, out var ip)) o.KnownProxies.Add(ip);
        });

        // ---- options
        services.Configure<MediaOptions>(configuration.GetSection(MediaOptions.SectionName));
        services.Configure<StorageOptions>(o =>
        {
            configuration.GetSection(StorageOptions.SectionName).Bind(o);
            if (string.IsNullOrWhiteSpace(o.ImagesPath)) o.ImagesPath = Path.Combine(env.WebRootPath, "uploads");
            if (string.IsNullOrWhiteSpace(o.DocumentsPath)) o.DocumentsPath = Path.Combine(env.ContentRootPath, "App_Data", "documents");
        });

        // ---- web services
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<IContentCacheInvalidator, ContentCacheInvalidator>();
        services.AddSingleton<IpHasher>();
        services.AddSingleton<FormTimestampProtector>();
        return services;
    }

    private static string ClientKey(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

/// <summary>Registers the localized display-name provider using the DI-managed localizer factory.</summary>
internal sealed class LocalizedMetadataSetup(IStringLocalizerFactory factory) : IConfigureOptions<MvcOptions>
{
    public void Configure(MvcOptions options)
    {
        options.ModelMetadataDetailsProviders.Add(new LocalizedDisplayMetadataProvider(factory));
        // Localize model-binding errors (evaluated per request, so the current UI culture applies).
        var l = factory.Create(typeof(SharedResource));
        var messages = options.ModelBindingMessageProvider;
        messages.SetAttemptedValueIsInvalidAccessor((value, field) => l["Val.InvalidFor", value, field]);
        messages.SetValueIsInvalidAccessor(value => l["Val.Invalid", value]);
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => l["Val.Invalid", value]);
        messages.SetUnknownValueIsInvalidAccessor(field => l["Val.InvalidOption"]);
        messages.SetValueMustBeANumberAccessor(field => l["Val.Number", field]);
        messages.SetValueMustNotBeNullAccessor(field => l["Val.Required", field]);
        messages.SetMissingBindRequiredValueAccessor(field => l["Val.Required", field]);
        messages.SetMissingKeyOrValueAccessor(() => l["Val.InvalidOption"]);
    }
}
