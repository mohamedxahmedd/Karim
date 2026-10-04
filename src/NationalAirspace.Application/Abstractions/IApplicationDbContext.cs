using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NationalAirspace.Domain.Entities;

namespace NationalAirspace.Application.Abstractions;

/// <summary>
/// Application view of the database. EF Core's <see cref="DbSet{TEntity}"/> already provides
/// repository / unit-of-work semantics, so no additional repository layer is introduced.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<MediaFile> MediaFiles { get; }
    DbSet<NewsCategory> NewsCategories { get; }
    DbSet<NewsArticle> NewsArticles { get; }
    DbSet<Announcement> Announcements { get; }
    DbSet<CenterService> CenterServices { get; }
    DbSet<ReportCategory> ReportCategories { get; }
    DbSet<Report> Reports { get; }
    DbSet<TrainingCourse> TrainingCourses { get; }
    DbSet<PhotoAlbum> PhotoAlbums { get; }
    DbSet<AlbumPhoto> AlbumPhotos { get; }
    DbSet<Video> Videos { get; }
    DbSet<Leader> Leaders { get; }
    DbSet<Department> Departments { get; }
    DbSet<LegalDocument> LegalDocuments { get; }
    DbSet<ContentPage> ContentPages { get; }
    DbSet<PageSection> PageSections { get; }
    DbSet<HeroSlide> HeroSlides { get; }
    DbSet<HomeSection> HomeSections { get; }
    DbSet<Statistic> Statistics { get; }
    DbSet<ImportantLink> ImportantLinks { get; }
    DbSet<MenuItem> MenuItems { get; }
    DbSet<SiteSettings> SiteSettings { get; }
    DbSet<ContactMessage> ContactMessages { get; }
    DbSet<AuditLog> AuditLogs { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
