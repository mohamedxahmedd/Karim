using NationalAirspace.Domain.Entities;
using NationalAirspace.Domain.Enums;
using NationalAirspace.Domain.ValueObjects;

namespace NationalAirspace.UnitTests.Domain;

public class VideoLinkTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    public void YouTube_links_use_privacy_enhanced_embed(string url)
    {
        Assert.True(VideoLink.TryCreate(url, out var link));
        Assert.Equal(VideoProvider.YouTube, link!.Provider);
        Assert.Equal("https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ", link.EmbedUrl);
    }

    [Fact]
    public void Vimeo_link_is_recognised()
    {
        Assert.True(VideoLink.TryCreate("https://vimeo.com/123456789", out var link));
        Assert.Equal(VideoProvider.Vimeo, link!.Provider);
    }

    [Theory]
    [InlineData("http://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("javascript:alert(1)")]
    [InlineData("not a url")]
    [InlineData("")]
    public void Non_https_or_invalid_links_are_rejected(string url) =>
        Assert.False(VideoLink.TryCreate(url, out _));
}

public class PublishingRulesTests
{
    private static readonly DateTime Now = new(2026, 10, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Draft_is_not_publicly_visible() =>
        Assert.False(new NewsArticle { Status = ContentStatus.Draft, PublishDateUtc = Now.AddDays(-1) }.IsPubliclyVisible(Now));

    [Fact]
    public void Published_item_with_future_date_is_scheduled_not_visible()
    {
        var article = new NewsArticle { Status = ContentStatus.Published, PublishDateUtc = Now.AddHours(1) };
        Assert.False(article.IsPubliclyVisible(Now));
        Assert.True(article.IsScheduled(Now));
    }

    [Fact]
    public void Published_item_with_past_date_is_visible() =>
        Assert.True(new NewsArticle { Status = ContentStatus.Published, PublishDateUtc = Now.AddMinutes(-1) }.IsPubliclyVisible(Now));

    [Fact]
    public void Expired_announcement_is_hidden_and_can_be_archived()
    {
        var announcement = new Announcement { Status = ContentStatus.Published, PublishDateUtc = Now.AddDays(-2), ExpiresAtUtc = Now.AddMinutes(-1) };
        Assert.False(announcement.IsPubliclyVisible(Now));
        Assert.True(announcement.ArchiveIfExpired(Now));
        Assert.Equal(ContentStatus.Archived, announcement.Status);
    }

    [Fact]
    public void Announcement_without_expiry_is_never_archived()
    {
        var announcement = new Announcement { Status = ContentStatus.Published, PublishDateUtc = Now.AddDays(-2) };
        Assert.False(announcement.ArchiveIfExpired(Now));
        Assert.True(announcement.IsPubliclyVisible(Now));
    }
}

public class DepartmentCycleTests
{
    private static readonly Guid Root = Guid.NewGuid(), Child = Guid.NewGuid(), Grandchild = Guid.NewGuid(), Other = Guid.NewGuid();
    private static readonly Dictionary<Guid, Guid?> Parents = new() { [Root] = null, [Child] = Root, [Grandchild] = Child, [Other] = null };

    [Fact]
    public void Moving_under_own_descendant_creates_cycle() => Assert.True(Department.WouldCreateCycle(Root, Grandchild, Parents));

    [Fact]
    public void Moving_under_itself_creates_cycle() => Assert.True(Department.WouldCreateCycle(Child, Child, Parents));

    [Fact]
    public void Moving_under_unrelated_department_is_allowed() => Assert.False(Department.WouldCreateCycle(Child, Other, Parents));

    [Fact]
    public void Moving_to_top_level_is_allowed() => Assert.False(Department.WouldCreateCycle(Grandchild, null, Parents));
}
