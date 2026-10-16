using System.Net;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Infrastructure.Persistence;

namespace NationalAirspace.IntegrationTests;

public sealed class TestClock : IClock
{
    public DateTime UtcNow { get; private set; } = new(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc);
    public TimeZoneInfo SiteTimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
    public void Advance(TimeSpan by) => UtcNow += by;
}

/// <summary>The site on a throwaway SQLite file, with a controllable clock.</summary>
public sealed class SiteFactory : WebApplicationFactory<Program>
{
    private readonly string _db = Path.Combine(Path.GetTempPath(), $"namc-it-{Guid.NewGuid():N}.db");
    public TestClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_db}");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Seed:SampleContent", "false");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IClock>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_db)) File.Delete(_db);
    }
}

public sealed class ContactFormTests(SiteFactory site) : IClassFixture<SiteFactory>
{
    [Fact]
    public async Task Submitting_the_contact_form_saves_the_message()
    {
        var client = site.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var page = await new HtmlParser().ParseDocumentAsync(await client.GetStringAsync("/ar/contact"));
        var form = page.Forms.Single(f => f.Method.Equals("post", StringComparison.OrdinalIgnoreCase) && f.Elements.Any(e => e.GetAttribute("name") == "Input.Message"));

        // Everything the page rendered (antiforgery token, form timestamp, empty honeypot) plus what a visitor types.
        var fields = form.Elements.OfType<IHtmlInputElement>().Where(i => !string.IsNullOrEmpty(i.Name))
            .ToDictionary(i => i.Name!, i => i.Value);
        fields["Input.Name"] = "زائر تجريبي";
        fields["Input.Email"] = "visitor@example.org";
        fields["Input.Subject"] = "استفسار";
        fields["Input.Message"] = "هذه رسالة تجريبية من نموذج التواصل.";
        site.Clock.Advance(TimeSpan.FromSeconds(30)); // a human takes longer than the bot threshold

        var response = await client.PostAsync(form.GetAttribute("action"), new FormUrlEncodedContent(fields));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("/ar/contact?sent=true", response.Headers.Location!.ToString());
        using var scope = site.Services.CreateScope();
        var saved = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().ContactMessages.SingleAsync();
        Assert.Equal("visitor@example.org", saved.Email);
    }
}
