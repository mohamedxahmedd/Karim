using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    /// <summary>Absolute path of the public images folder (served at /uploads).</summary>
    public string ImagesPath { get; set; } = string.Empty;
    /// <summary>Absolute path of the private documents folder (never served statically).</summary>
    public string DocumentsPath { get; set; } = string.Empty;
}

/// <summary>Stores files on the local file system using random names in yyyy/MM folders.</summary>
public sealed partial class LocalFileStorage(IOptions<StorageOptions> options, IClock clock) : IFileStorage
{
    private readonly StorageOptions _options = options.Value;

    public async Task<string> SaveAsync(Stream content, MediaKind kind, string extension, CancellationToken cancellationToken = default)
    {
        extension = extension.ToLowerInvariant();
        if (!ExtensionPattern().IsMatch(extension)) throw new ArgumentException("Invalid extension.", nameof(extension));

        var now = clock.UtcNow;
        var relative = $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension}";
        var fullPath = Resolve(relative, kind) ?? throw new InvalidOperationException("Could not resolve storage path.");
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
        return relative;
    }

    public Task DeleteAsync(string storagePath, MediaKind kind, CancellationToken cancellationToken = default)
    {
        var fullPath = Resolve(storagePath, kind);
        if (fullPath is not null && File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Stream? OpenRead(string storagePath, MediaKind kind)
    {
        var fullPath = Resolve(storagePath, kind);
        return fullPath is not null && File.Exists(fullPath)
            ? new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true)
            : null;
    }

    /// <summary>Maps a storage path to an absolute path, rejecting anything outside the expected shape/root.</summary>
    private string? Resolve(string storagePath, MediaKind kind)
    {
        if (!StoragePathPattern().IsMatch(storagePath)) return null;
        var root = Path.GetFullPath(kind == MediaKind.Image ? _options.ImagesPath : _options.DocumentsPath);
        var full = Path.GetFullPath(Path.Combine(root, storagePath));
        return full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? full : null;
    }

    [GeneratedRegex(@"^\d{4}/\d{2}/[a-f0-9]{32}\.[a-z0-9]{2,5}$")]
    private static partial Regex StoragePathPattern();

    [GeneratedRegex(@"^\.[a-z0-9]{2,5}$")]
    private static partial Regex ExtensionPattern();
}
