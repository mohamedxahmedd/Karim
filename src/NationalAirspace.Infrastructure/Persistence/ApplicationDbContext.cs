using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Domain.Common;
using NationalAirspace.Domain.Entities;
using NationalAirspace.Infrastructure.Identity;

namespace NationalAirspace.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IClock clock)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IApplicationDbContext
{
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<NewsCategory> NewsCategories => Set<NewsCategory>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<CenterService> CenterServices => Set<CenterService>();
    public DbSet<ReportCategory> ReportCategories => Set<ReportCategory>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<TrainingCourse> TrainingCourses => Set<TrainingCourse>();
    public DbSet<PhotoAlbum> PhotoAlbums => Set<PhotoAlbum>();
    public DbSet<AlbumPhoto> AlbumPhotos => Set<AlbumPhoto>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Leader> Leaders => Set<Leader>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LegalDocument> LegalDocuments => Set<LegalDocument>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<PageSection> PageSections => Set<PageSection>();
    public DbSet<HeroSlide> HeroSlides => Set<HeroSlide>();
    public DbSet<HomeSection> HomeSections => Set<HomeSection>();
    public DbSet<Statistic> Statistics => Set<Statistic>();
    public DbSet<ImportantLink> ImportantLinks => Set<ImportantLink>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // All DateTime values are persisted and materialized as UTC.
        var utc = new ValueConverter<DateTime, DateTime>(v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                                                         v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        var utcNullable = new ValueConverter<DateTime?, DateTime?>(v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()) : v,
                                                                   v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime)) property.SetValueConverter(utc);
                else if (property.ClrType == typeof(DateTime?)) property.SetValueConverter(utcNullable);
            }

            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
                builder.Entity(entityType.ClrType).Property(nameof(AuditableEntity.ConcurrencyStamp)).IsConcurrencyToken();
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    private void StampEntities()
    {
        var now = clock.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.UpdatedAtUtc = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = now;
                    entry.Entity.ConcurrencyStamp = Guid.NewGuid();
                    break;
            }
        }
    }
}
