using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Application.Features.Media;
using NationalAirspace.Domain.Enums;
using NationalAirspace.Infrastructure.Persistence;
using NationalAirspace.Infrastructure.Services;

namespace NationalAirspace.UnitTests.Support;

public sealed class FakeClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; set; } = utcNow;
    public TimeZoneInfo SiteTimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
}

public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; } = Guid.NewGuid();
    public string UserName => "tester@example.org";
    public string? IpAddress => "127.0.0.1";
}

public sealed class CountingCacheInvalidator : IContentCacheInvalidator
{
    public int Calls { get; private set; }
    public Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryFileStorage : IFileStorage
{
    public Dictionary<string, byte[]> Files { get; } = [];

    public async Task<string> SaveAsync(Stream content, MediaKind kind, string extension, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var path = $"2026/10/{Guid.NewGuid():N}{extension}";
        Files[path] = buffer.ToArray();
        return path;
    }

    public Task DeleteAsync(string storagePath, MediaKind kind, CancellationToken cancellationToken = default)
    {
        Files.Remove(storagePath);
        return Task.CompletedTask;
    }

    public Stream? OpenRead(string storagePath, MediaKind kind) =>
        Files.TryGetValue(storagePath, out var bytes) ? new MemoryStream(bytes) : null;
}

/// <summary>A fresh SQLite in-memory database (real relational behaviour: constraints, concurrency tokens).</summary>
public sealed class TestHost : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public FakeClock Clock { get; } = new(new DateTime(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc));
    public FakeCurrentUser User { get; } = new();
    public CountingCacheInvalidator Cache { get; } = new();
    public InMemoryFileStorage Storage { get; } = new();
    public ApplicationDbContext Db { get; }

    public TestHost()
    {
        _connection.Open();
        Db = NewContext();
        Db.Database.EnsureCreated();
    }

    /// <summary>A second context on the same database (simulates another request / another editor).</summary>
    public ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options, Clock);

    public AdminServiceDependencies Deps(ApplicationDbContext? db = null) =>
        new(db ?? Db, Clock, User, new HtmlSanitizerService(), Cache);

    public MediaService Media(ApplicationDbContext? db = null) =>
        new(Deps(db), Storage, Options.Create(new MediaOptions { MaxImageBytes = 1024 * 1024, MaxDocumentBytes = 2 * 1024 * 1024 }));

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

public static class SampleBytes
{
    public static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52];
    public static readonly byte[] PdfHeader = "%PDF-1.7\n"u8.ToArray();
    public static readonly byte[] WindowsExe = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00];

    public static MemoryStream Of(byte[] header, int totalLength = 256)
    {
        var bytes = new byte[Math.Max(totalLength, header.Length)];
        header.CopyTo(bytes, 0);
        return new MemoryStream(bytes);
    }
}
