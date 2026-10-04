using Microsoft.EntityFrameworkCore;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Features.Contact;
using NationalAirspace.Domain.Enums;

namespace NationalAirspace.Application.Features.Dashboard;

public sealed record CountPoint(string Label, int Value);

public sealed record ActivityItem(DateTime CreatedAtUtc, string UserName, string Action, string EntityType, string? Summary);

public sealed record DashboardDto(
    int TotalNews, int PublishedNews, int DraftNews, int ScheduledNews,
    int Services, int Reports, int TrainingCourses, int ActiveAnnouncements, int Announcements,
    int MediaItems, int ContactMessages, int NewMessages,
    /// <summary>Not tracked by the CMS; populated when an analytics provider is integrated.</summary>
    int? WebsiteVisitors,
    IReadOnlyList<CountPoint> NewsPerMonth,
    IReadOnlyList<CountPoint> MessagesPerMonth,
    IReadOnlyList<ContactMessageListItem> RecentMessages,
    IReadOnlyList<ActivityItem> RecentActivity);

public sealed class DashboardService(IApplicationDbContext db, IClock clock)
{
    public async Task<DashboardDto> GetAsync(bool includeMessages, bool includeActivity, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var news = db.NewsArticles.AsNoTracking();
        var totalNews = await news.CountAsync(ct);
        var publishedNews = await news.CountAsync(n => n.Status == ContentStatus.Published && n.PublishDateUtc <= now, ct);
        var scheduledNews = await news.CountAsync(n => n.Status == ContentStatus.Published && n.PublishDateUtc > now, ct);
        var draftNews = await news.CountAsync(n => n.Status == ContentStatus.Draft, ct);

        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var newsDates = await news.Where(n => n.PublishDateUtc >= monthStart && n.Status != ContentStatus.Draft)
            .Select(n => n.PublishDateUtc!.Value).ToListAsync(ct);
        var messageDates = await db.ContactMessages.AsNoTracking().Where(m => m.CreatedAtUtc >= monthStart).Select(m => m.CreatedAtUtc).ToListAsync(ct);

        IReadOnlyList<CountPoint> PerMonth(List<DateTime> dates) => Enumerable.Range(0, 6).Select(i =>
        {
            var start = monthStart.AddMonths(i);
            return new CountPoint(clock.ToSiteTime(start).ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
                dates.Count(d => d >= start && d < start.AddMonths(1)));
        }).ToList();

        var recentMessages = includeMessages
            ? await db.ContactMessages.AsNoTracking().OrderByDescending(m => m.CreatedAtUtc).Take(5)
                .Select(m => new ContactMessageListItem(m.Id, m.Name, m.Email, m.Subject, m.Status, m.CreatedAtUtc)).ToListAsync(ct)
            : [];
        var activity = includeActivity
            ? await db.AuditLogs.AsNoTracking().OrderByDescending(a => a.CreatedAtUtc).Take(8)
                .Select(a => new ActivityItem(a.CreatedAtUtc, a.UserName, a.Action, a.EntityType, a.Summary)).ToListAsync(ct)
            : [];

        return new DashboardDto(
            totalNews, publishedNews, draftNews, scheduledNews,
            await db.CenterServices.CountAsync(ct),
            await db.Reports.CountAsync(ct),
            await db.TrainingCourses.CountAsync(ct),
            await db.Announcements.CountAsync(a => a.Status == ContentStatus.Published && a.PublishDateUtc <= now && (a.ExpiresAtUtc == null || a.ExpiresAtUtc > now), ct),
            await db.Announcements.CountAsync(ct),
            await db.MediaFiles.CountAsync(ct) + await db.Videos.CountAsync(ct),
            await db.ContactMessages.CountAsync(ct),
            await db.ContactMessages.CountAsync(m => m.Status == ContactMessageStatus.New, ct),
            WebsiteVisitors: null,
            PerMonth(newsDates), PerMonth(messageDates), recentMessages, activity);
    }
}
