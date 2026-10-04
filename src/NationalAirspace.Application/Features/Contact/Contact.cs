using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Contact;

public class ContactInput
{
    [Required(ErrorMessage = V.Required), StringLength(150, ErrorMessage = V.MaxLength)] public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), EmailAddress(ErrorMessage = V.Email), StringLength(256, ErrorMessage = V.MaxLength)] public string Email { get; set; } = string.Empty;
    [StringLength(30, ErrorMessage = V.MaxLength), RegularExpression(@"^[0-9+()\-\s]{6,30}$", ErrorMessage = V.Phone)] public string? Phone { get; set; }
    [StringLength(200, ErrorMessage = V.MaxLength)] public string? Organization { get; set; }
    [Required(ErrorMessage = V.Required), StringLength(200, ErrorMessage = V.MaxLength)] public string Subject { get; set; } = string.Empty;
    [Required(ErrorMessage = V.Required), StringLength(4000, MinimumLength = 10, ErrorMessage = V.MinLength)] public string Message { get; set; } = string.Empty;
}

public sealed record ContactMessageListItem(Guid Id, string Name, string Email, string Subject, ContactMessageStatus Status, DateTime CreatedAtUtc);

public sealed record ContactMessageDetail(Guid Id, string Name, string Email, string? Phone, string? Organization, string Subject, string Message,
    ContactMessageStatus Status, DateTime CreatedAtUtc, DateTime? ReadAtUtc, DateTime? RepliedAtUtc, string? AdminNotes, string? Culture);

/// <summary>Heuristics for automated submissions (honeypot + human-plausible fill time).</summary>
public static class ContactSpamCheck
{
    public static readonly TimeSpan MinimumFillTime = TimeSpan.FromSeconds(3);
    public static readonly TimeSpan MaximumFormAge = TimeSpan.FromHours(6);

    public static bool IsLikelyBot(string? honeypot, DateTime? formRenderedAtUtc, DateTime nowUtc)
    {
        if (!string.IsNullOrEmpty(honeypot)) return true;
        if (formRenderedAtUtc is not { } rendered) return true;
        var elapsed = nowUtc - rendered;
        return elapsed < MinimumFillTime || elapsed > MaximumFormAge;
    }
}

public sealed class ContactService(IApplicationDbContext db, IClock clock)
{
    public const int MaxMessagesPerSenderPerHour = 5;

    /// <param name="ipHash">Keyed hash of the sender IP (raw IPs are never stored).</param>
    public async Task<Result> SubmitAsync(ContactInput input, string? ipHash, string culture, CancellationToken ct = default)
    {
        var errors = InputValidator.Validate(input);
        if (errors.Count > 0) return Result.Failure(errors);

        var now = clock.UtcNow;
        var since = now.AddHours(-1);
        var email = input.Email.Trim();
        var recent = await db.ContactMessages.CountAsync(m => m.CreatedAtUtc >= since && ((ipHash != null && m.IpHash == ipHash) || m.Email == email), ct);
        if (recent >= MaxMessagesPerSenderPerHour) return Result.Failure(new Error("Contact.TooMany"));

        db.ContactMessages.Add(new ContactMessage
        {
            Name = input.Name.Trim(),
            Email = email,
            Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim(),
            Organization = string.IsNullOrWhiteSpace(input.Organization) ? null : input.Organization.Trim(),
            Subject = input.Subject.Trim(),
            Message = input.Message.Trim(),
            IpHash = ipHash,
            Culture = culture,
        });
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class ContactAdminService(AdminServiceDependencies deps) : AdminServiceBase(deps)
{
    protected override string AuditEntityType => "ContactMessage";

    public async Task<PagedResult<ContactMessageListItem>> ListAsync(string? search, ContactMessageStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Db.ContactMessages.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(m => m.Name.Contains(term) || m.Email.Contains(term) || m.Subject.Contains(term) || (m.Organization != null && m.Organization.Contains(term)));
        }
        if (status is { } s) query = query.Where(m => m.Status == s);
        return await query.OrderByDescending(m => m.CreatedAtUtc)
            .Select(m => new ContactMessageListItem(m.Id, m.Name, m.Email, m.Subject, m.Status, m.CreatedAtUtc))
            .ToPagedAsync(page, pageSize, ct);
    }

    /// <summary>Returns the message and marks it as read.</summary>
    public async Task<ContactMessageDetail?> OpenAsync(Guid id, CancellationToken ct = default)
    {
        var m = await Db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return null;
        if (m.Status == ContactMessageStatus.New)
        {
            m.MarkRead(Clock.UtcNow);
            await Db.SaveChangesAsync(ct);
        }
        return new ContactMessageDetail(m.Id, m.Name, m.Email, m.Phone, m.Organization, m.Subject, m.Message, m.Status,
            m.CreatedAtUtc, m.ReadAtUtc, m.RepliedAtUtc, m.AdminNotes, m.Culture);
    }

    public async Task<Result> SetStatusAsync(Guid id, ContactMessageStatus status, CancellationToken ct = default)
    {
        var m = await Db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return Result.Failure(Error.NotFound);
        switch (status)
        {
            case ContactMessageStatus.Replied: m.MarkReplied(Clock.UtcNow); break;
            case ContactMessageStatus.Read: m.Status = ContactMessageStatus.Read; m.ReadAtUtc ??= Clock.UtcNow; break;
            default: m.Status = ContactMessageStatus.New; break;
        }
        Audit($"Status:{status}", id, m.Subject);
        return await CommitAsync(ct);
    }

    public async Task<Result> SaveNotesAsync(Guid id, string? notes, CancellationToken ct = default)
    {
        if (notes?.Length > 1000) return Result.Failure(new Error(V.MaxLength, "AdminNotes"));
        var m = await Db.ContactMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return Result.Failure(Error.NotFound);
        m.AdminNotes = Clean(notes);
        Audit("Notes", id, m.Subject);
        return await CommitAsync(ct);
    }

    public Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) => DeleteAsync(Db.ContactMessages, id, x => x.Subject, ct);

    public Task<int> CountNewAsync(CancellationToken ct = default) => Db.ContactMessages.CountAsync(m => m.Status == ContactMessageStatus.New, ct);
}
