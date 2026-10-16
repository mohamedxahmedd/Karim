using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NationalAirspace.Domain.Entities;
using L = NationalAirspace.Infrastructure.Persistence.Configurations.Lengths;

namespace NationalAirspace.Infrastructure.Persistence.Configurations;

internal sealed class HeroSlideConfiguration : IEntityTypeConfiguration<HeroSlide>
{
    public void Configure(EntityTypeBuilder<HeroSlide> b)
    {
        b.Property(x => x.TitleAr).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.SubtitleAr).HasMaxLength(L.Title);
        b.Property(x => x.SubtitleEn).HasMaxLength(L.Title);
        b.Property(x => x.DescriptionAr).HasMaxLength(L.Short);
        b.Property(x => x.DescriptionEn).HasMaxLength(L.Short);
        b.Property(x => x.PrimaryButtonTextAr).HasMaxLength(L.Small);
        b.Property(x => x.PrimaryButtonTextEn).HasMaxLength(L.Small);
        b.Property(x => x.PrimaryButtonUrl).HasMaxLength(L.Url);
        b.Property(x => x.SecondaryButtonTextAr).HasMaxLength(L.Small);
        b.Property(x => x.SecondaryButtonTextEn).HasMaxLength(L.Small);
        b.Property(x => x.SecondaryButtonUrl).HasMaxLength(L.Url);
        b.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class HomeSectionConfiguration : IEntityTypeConfiguration<HomeSection>
{
    public void Configure(EntityTypeBuilder<HomeSection> b)
    {
        b.Property(x => x.TitleAr).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Title).IsRequired();
        b.Property(x => x.SubtitleAr).HasMaxLength(L.Title);
        b.Property(x => x.SubtitleEn).HasMaxLength(L.Title);
        b.Property(x => x.DescriptionAr).HasMaxLength(L.Long);
        b.Property(x => x.DescriptionEn).HasMaxLength(L.Long);
        b.Property(x => x.ButtonTextAr).HasMaxLength(L.Small);
        b.Property(x => x.ButtonTextEn).HasMaxLength(L.Small);
        b.Property(x => x.ButtonUrl).HasMaxLength(L.Url);
        b.HasIndex(x => x.Key).IsUnique();
        b.HasOne(x => x.Image).WithMany().HasForeignKey(x => x.ImageId).OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class StatisticConfiguration : IEntityTypeConfiguration<Statistic>
{
    public void Configure(EntityTypeBuilder<Statistic> b)
    {
        b.Property(x => x.LabelAr).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.LabelEn).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.Value).HasMaxLength(30).IsRequired();
        b.Property(x => x.Icon).HasMaxLength(L.Icon);
    }
}

internal sealed class ImportantLinkConfiguration : IEntityTypeConfiguration<ImportantLink>
{
    public void Configure(EntityTypeBuilder<ImportantLink> b)
    {
        b.Property(x => x.TitleAr).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.Url).HasMaxLength(L.Url).IsRequired();
        b.Property(x => x.Icon).HasMaxLength(L.Icon);
    }
}

internal sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> b)
    {
        b.Property(x => x.TitleAr).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.TitleEn).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.Url).HasMaxLength(L.Url).IsRequired();
        b.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.NoAction);
        b.HasIndex(x => new { x.Location, x.ParentId, x.DisplayOrder });
    }
}

internal sealed class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> b)
    {
        b.Property(x => x.SiteNameAr).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.SiteNameEn).HasMaxLength(L.Name).IsRequired();
        b.Property(x => x.TaglineAr).HasMaxLength(L.Title);
        b.Property(x => x.TaglineEn).HasMaxLength(L.Title);
        b.Property(x => x.AddressAr).HasMaxLength(L.Title);
        b.Property(x => x.AddressEn).HasMaxLength(L.Title);
        b.Property(x => x.Phone).HasMaxLength(50);
        b.Property(x => x.Fax).HasMaxLength(50);
        b.Property(x => x.Email).HasMaxLength(256);
        b.Property(x => x.WorkingHoursAr).HasMaxLength(L.Title);
        b.Property(x => x.WorkingHoursEn).HasMaxLength(L.Title);
        b.Property(x => x.MapEmbedUrl).HasMaxLength(1000);
        b.Property(x => x.FacebookUrl).HasMaxLength(L.Url);
        b.Property(x => x.XUrl).HasMaxLength(L.Url);
        b.Property(x => x.LinkedInUrl).HasMaxLength(L.Url);
        b.Property(x => x.YouTubeUrl).HasMaxLength(L.Url);
        b.Property(x => x.InstagramUrl).HasMaxLength(L.Url);
        b.Property(x => x.FooterTextAr).HasMaxLength(L.Short);
        b.Property(x => x.FooterTextEn).HasMaxLength(L.Short);
        b.Property(x => x.FooterLinksTitleAr).HasMaxLength(L.Title);
        b.Property(x => x.FooterLinksTitleEn).HasMaxLength(L.Title);
        b.Property(x => x.FooterContactTitleAr).HasMaxLength(L.Title);
        b.Property(x => x.FooterContactTitleEn).HasMaxLength(L.Title);
        b.Property(x => x.FooterCopyrightAr).HasMaxLength(L.Title);
        b.Property(x => x.FooterCopyrightEn).HasMaxLength(L.Title);
        b.Property(x => x.DefaultMetaDescriptionAr).HasMaxLength(L.MetaDescription);
        b.Property(x => x.DefaultMetaDescriptionEn).HasMaxLength(L.MetaDescription);
    }
}

internal sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> b)
    {
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(30);
        b.Property(x => x.Organization).HasMaxLength(200);
        b.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        b.Property(x => x.Message).HasMaxLength(L.Long).IsRequired();
        b.Property(x => x.AdminNotes).HasMaxLength(L.Short);
        b.Property(x => x.IpHash).HasMaxLength(64);
        b.Property(x => x.Culture).HasMaxLength(10);
        b.HasIndex(x => new { x.Status, x.CreatedAtUtc });
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.Property(x => x.UserName).HasMaxLength(256).IsRequired();
        b.Property(x => x.Action).HasMaxLength(64).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(64);
        b.Property(x => x.Summary).HasMaxLength(400);
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.HasIndex(x => x.CreatedAtUtc);
        b.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<Identity.ApplicationUser>
{
    public void Configure(EntityTypeBuilder<Identity.ApplicationUser> b)
    {
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
    }
}
