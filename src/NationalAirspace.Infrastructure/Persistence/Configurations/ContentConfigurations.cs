using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NationalAirspace.Domain.Entities;
using L = NationalAirspace.Infrastructure.Persistence.Configurations.Lengths;

namespace NationalAirspace.Infrastructure.Persistence.Configurations;

// Media foreign keys are NO ACTION everywhere: SQL Server rejects multiple cascade paths, and the
// media service refuses to delete files that are still referenced.

internal sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> b)
    {
        b.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.StoragePath).HasMaxLength(300).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        b.Property(x => x.AltTextAr).HasMaxLength(250);
        b.Property(x => x.AltTextEn).HasMaxLength(250);
        b.HasIndex(x => x.StoragePath).IsUnique();
        b.HasIndex(x => new { x.Kind, x.CreatedAtUtc });
    }
}

internal sealed class NewsCategoryConfiguration : IEntityTypeConfiguration<NewsCategory>
{
    public void Configure(EntityTypeBuilder<NewsCategory> b)
    {
        b.Property(x => x.NameAr).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(L.Slug).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

internal sealed class NewsArticleConfiguration : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> b)
    {
        b.Property(x => x.TitleAr).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.SummaryAr).HasMaxLength(L.Short);
        b.Property(x => x.SummaryEn).HasMaxLength(L.Short);
        b.Property(x => x.SlugAr).HasMaxLength(L.Slug).IsRequired();
        b.Property(x => x.SlugEn).HasMaxLength(L.Slug).IsRequired();
        b.Property(x => x.AuthorAr).HasMaxLength(150);
        b.Property(x => x.AuthorEn).HasMaxLength(150);
        b.Property(x => x.SeoTitleAr).HasMaxLength(L.SeoTitle);
        b.Property(x => x.SeoTitleEn).HasMaxLength(L.SeoTitle);
        b.Property(x => x.MetaDescriptionAr).HasMaxLength(L.MetaDescription);
        b.Property(x => x.MetaDescriptionEn).HasMaxLength(L.MetaDescription);
        b.HasIndex(x => x.SlugAr).IsUnique();
        b.HasIndex(x => x.SlugEn).IsUnique();
        b.HasIndex(x => new { x.Status, x.PublishDateUtc });
        b.HasOne(x => x.NewsCategory).WithMany(c => c.Articles).HasForeignKey(x => x.NewsCategoryId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.FeaturedImage).WithMany().HasForeignKey(x => x.FeaturedImageId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> b)
    {
        b.Property(x => x.TitleAr).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Title).IsRequired();
        b.HasIndex(x => new { x.Status, x.PublishDateUtc, x.ExpiresAtUtc });
        b.HasOne(x => x.Attachment).WithMany().HasForeignKey(x => x.AttachmentId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class CenterServiceConfiguration : IEntityTypeConfiguration<CenterService>
{
    public void Configure(EntityTypeBuilder<CenterService> b)
    {
        b.Property(x => x.TitleAr).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.ShortDescriptionAr).HasMaxLength(L.Short);
        b.Property(x => x.ShortDescriptionEn).HasMaxLength(L.Short);
        b.Property(x => x.Icon).HasMaxLength(L.Icon);
        b.Property(x => x.SlugAr).HasMaxLength(L.Slug).IsRequired();
        b.Property(x => x.SlugEn).HasMaxLength(L.Slug).IsRequired();
        b.Property(x => x.SeoTitleAr).HasMaxLength(L.SeoTitle);
        b.Property(x => x.SeoTitleEn).HasMaxLength(L.SeoTitle);
        b.Property(x => x.MetaDescriptionAr).HasMaxLength(L.MetaDescription);
        b.Property(x => x.MetaDescriptionEn).HasMaxLength(L.MetaDescription);
        b.HasIndex(x => x.SlugAr).IsUnique();
        b.HasIndex(x => x.SlugEn).IsUnique();
        b.HasIndex(x => new { x.Status, x.DisplayOrder });
        b.HasOne(x => x.CoverImage).WithMany().HasForeignKey(x => x.CoverImageId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ReportCategoryConfiguration : IEntityTypeConfiguration<ReportCategory>
{
    public void Configure(EntityTypeBuilder<ReportCategory> b)
    {
        b.Property(x => x.NameAr).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(L.Slug).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

internal sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> b)
    {
        b.Property(x => x.TitleAr).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.DescriptionAr).HasMaxLength(L.Long);
        b.Property(x => x.DescriptionEn).HasMaxLength(L.Long);
        b.Property(x => x.SlugAr).HasMaxLength(L.Slug).IsRequired();
        b.Property(x => x.SlugEn).HasMaxLength(L.Slug).IsRequired();
        b.Property(x => x.SeoTitleAr).HasMaxLength(L.SeoTitle);
        b.Property(x => x.SeoTitleEn).HasMaxLength(L.SeoTitle);
        b.Property(x => x.MetaDescriptionAr).HasMaxLength(L.MetaDescription);
        b.Property(x => x.MetaDescriptionEn).HasMaxLength(L.MetaDescription);
        b.HasIndex(x => x.SlugAr).IsUnique();
        b.HasIndex(x => x.SlugEn).IsUnique();
        b.HasIndex(x => new { x.Status, x.PublicationDate });
        b.HasOne(x => x.ReportCategory).WithMany(c => c.Reports).HasForeignKey(x => x.ReportCategoryId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.PdfFile).WithMany().HasForeignKey(x => x.PdfFileId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.CoverImage).WithMany().HasForeignKey(x => x.CoverImageId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class TrainingCourseConfiguration : IEntityTypeConfiguration<TrainingCourse>
{
    public void Configure(EntityTypeBuilder<TrainingCourse> b)
    {
        b.Property(x => x.TitleAr).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.SlugAr).HasMaxLength(L.Slug).IsRequired();
        b.Property(x => x.SlugEn).HasMaxLength(L.Slug).IsRequired();
        b.Property(x => x.DurationAr).HasMaxLength(L.Small);
        b.Property(x => x.DurationEn).HasMaxLength(L.Small);
        b.Property(x => x.TargetAudienceAr).HasMaxLength(L.Short);
        b.Property(x => x.TargetAudienceEn).HasMaxLength(L.Short);
        b.Property(x => x.LocationAr).HasMaxLength(L.Name);
        b.Property(x => x.LocationEn).HasMaxLength(L.Name);
        b.HasIndex(x => x.SlugAr).IsUnique();
        b.HasIndex(x => x.SlugEn).IsUnique();
        b.HasIndex(x => new { x.Status, x.StartDate });
        b.HasOne(x => x.CourseImage).WithMany().HasForeignKey(x => x.CourseImageId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne(x => x.Attachment).WithMany().HasForeignKey(x => x.AttachmentId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class PhotoAlbumConfiguration : IEntityTypeConfiguration<PhotoAlbum>
{
    public void Configure(EntityTypeBuilder<PhotoAlbum> b)
    {
        b.Property(x => x.TitleAr).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.DescriptionAr).HasMaxLength(L.Long);
        b.Property(x => x.DescriptionEn).HasMaxLength(L.Long);
        b.HasOne(x => x.CoverMedia).WithMany().HasForeignKey(x => x.CoverMediaId).OnDelete(DeleteBehavior.NoAction);
        b.HasMany(x => x.Photos).WithOne(p => p.Album).HasForeignKey(p => p.AlbumId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AlbumPhotoConfiguration : IEntityTypeConfiguration<AlbumPhoto>
{
    public void Configure(EntityTypeBuilder<AlbumPhoto> b)
    {
        b.Property(x => x.CaptionAr).HasMaxLength(L.Title);
        b.Property(x => x.CaptionEn).HasMaxLength(L.Title);
        b.HasIndex(x => new { x.AlbumId, x.DisplayOrder });
        b.HasOne(x => x.MediaFile).WithMany().HasForeignKey(x => x.MediaFileId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> b)
    {
        b.Property(x => x.TitleAr).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.DescriptionAr).HasMaxLength(L.Long);
        b.Property(x => x.DescriptionEn).HasMaxLength(L.Long);
        b.Property(x => x.Url).HasMaxLength(L.Url).IsRequired();
        b.Property(x => x.ExternalId).HasMaxLength(64);
        b.HasOne(x => x.Thumbnail).WithMany().HasForeignKey(x => x.ThumbnailId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class LeaderConfiguration : IEntityTypeConfiguration<Leader>
{
    public void Configure(EntityTypeBuilder<Leader> b)
    {
        b.Property(x => x.NameAr).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.PositionAr).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.PositionEn).HasMaxLength(L.Name).IsRequired();
        b.HasOne(x => x.Photo).WithMany().HasForeignKey(x => x.PhotoId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.Property(x => x.NameAr).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.DescriptionAr).HasMaxLength(L.Long);
        b.Property(x => x.DescriptionEn).HasMaxLength(L.Long);
        b.HasOne(x => x.ParentDepartment).WithMany(x => x.Children).HasForeignKey(x => x.ParentDepartmentId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.ParentDepartmentId, x.DisplayOrder });
    }
}

internal sealed class LegalDocumentConfiguration : IEntityTypeConfiguration<LegalDocument>
{
    public void Configure(EntityTypeBuilder<LegalDocument> b)
    {
        b.Property(x => x.TitleAr).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.DescriptionAr).HasMaxLength(L.Long);
        b.Property(x => x.DescriptionEn).HasMaxLength(L.Long);
        b.Property(x => x.DocumentNumber).HasMaxLength(L.Small);
        b.HasIndex(x => new { x.Status, x.Category, x.PublicationDate });
        b.HasOne(x => x.DocumentFile).WithMany().HasForeignKey(x => x.DocumentFileId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ContentPageConfiguration : IEntityTypeConfiguration<ContentPage>
{
    public void Configure(EntityTypeBuilder<ContentPage> b)
    {
        b.Property(x => x.SystemKey).HasMaxLength(L.Small);
        b.Property(x => x.TitleAr).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.SlugAr).HasMaxLength(L.Slug).IsRequired();
        b.Property(x => x.SlugEn).HasMaxLength(L.Slug).IsRequired();
        b.Property(x => x.SummaryAr).HasMaxLength(L.Short);
        b.Property(x => x.SummaryEn).HasMaxLength(L.Short);
        b.Property(x => x.SeoTitleAr).HasMaxLength(L.SeoTitle);
        b.Property(x => x.SeoTitleEn).HasMaxLength(L.SeoTitle);
        b.Property(x => x.MetaDescriptionAr).HasMaxLength(L.MetaDescription);
        b.Property(x => x.MetaDescriptionEn).HasMaxLength(L.MetaDescription);
        b.HasIndex(x => x.SystemKey).IsUnique();
        b.HasIndex(x => x.SlugAr).IsUnique();
        b.HasIndex(x => x.SlugEn).IsUnique();
        b.HasOne(x => x.HeroImage).WithMany().HasForeignKey(x => x.HeroImageId).OnDelete(DeleteBehavior.NoAction);
        b.HasMany(x => x.Sections).WithOne(s => s.Page).HasForeignKey(s => s.PageId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PageSectionConfiguration : IEntityTypeConfiguration<PageSection>
{
    public void Configure(EntityTypeBuilder<PageSection> b)
    {
        b.Property(x => x.Key).HasMaxLength(L.Small);
        b.Property(x => x.TitleAr).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.Icon).HasMaxLength(L.Icon);
        b.HasIndex(x => new { x.PageId, x.DisplayOrder });
        b.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageId).OnDelete(DeleteBehavior.NoAction);
    }
}
