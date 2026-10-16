using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NationalAirspace.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// The live map replaces the demo dashboard: default menu items and hero buttons that still point to the demo
    /// (with their seeded titles) now point to /live-airspace. Items an editor renamed are left alone; the demo URL
    /// itself redirects to the live map.
    /// </summary>
    public partial class PointDemoLinksToLiveMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE [MenuItems] SET [TitleAr] = N'خريطة المجال الجوي الحية', [TitleEn] = N'Live Airspace Map', [Url] = N'/live-airspace'
WHERE [Url] = N'/airspace-demo' AND [TitleAr] = N'لوحة العرض التوضيحية';");
            migrationBuilder.Sql(@"UPDATE [HeroSlides] SET [SecondaryButtonTextAr] = N'الخريطة الحية', [SecondaryButtonTextEn] = N'Live map', [SecondaryButtonUrl] = N'/live-airspace'
WHERE [SecondaryButtonUrl] = N'/airspace-demo' AND [SecondaryButtonTextAr] = N'لوحة العرض التوضيحية';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE [MenuItems] SET [TitleAr] = N'لوحة العرض التوضيحية', [TitleEn] = N'Demo Dashboard', [Url] = N'/airspace-demo'
WHERE [Url] = N'/live-airspace' AND [TitleAr] = N'خريطة المجال الجوي الحية';");
            migrationBuilder.Sql(@"UPDATE [HeroSlides] SET [SecondaryButtonTextAr] = N'لوحة العرض التوضيحية', [SecondaryButtonTextEn] = N'Demo dashboard', [SecondaryButtonUrl] = N'/airspace-demo'
WHERE [SecondaryButtonUrl] = N'/live-airspace' AND [SecondaryButtonTextAr] = N'الخريطة الحية';");
        }
    }
}
