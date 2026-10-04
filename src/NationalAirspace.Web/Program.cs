using Microsoft.Extensions.Options;
using NationalAirspace.Application;
using NationalAirspace.Application.Common;
using NationalAirspace.Infrastructure;
using NationalAirspace.Infrastructure.Persistence.Seeding;
using NationalAirspace.Web.Extensions;
using NationalAirspace.Web.Localization;
using NationalAirspace.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath)
    .AddWeb(builder.Configuration, builder.Environment);

var app = builder.Build();

if (builder.Configuration.GetValue("ReverseProxy:Enabled", false))
    app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStatusCodePagesWithReExecute("/error/{0}");
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Uploaded files have random immutable names; library assets are versioned with asp-append-version.
        if (ctx.Context.Request.Path.StartsWithSegments(MediaUrls.ImagesRequestPath))
            ctx.Context.Response.Headers.CacheControl = "public,max-age=2592000,immutable";
    },
});

app.UseRouting();
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapHealthChecks("/health");
app.MapControllerRoute("admin", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
}

app.Run();

/// <summary>Entry point marker for integration tests (WebApplicationFactory).</summary>
public partial class Program;
