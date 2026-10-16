using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NationalAirspace.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Removes the demo dashboard, the live airspace map and the footer notice: their tables and settings columns,
    /// the menu items and hero buttons that pointed to them, and the now-lone "Overview" child under Airspace Management.
    /// </summary>
    public partial class RemoveDemoAndLiveAirspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DELETE FROM [MenuItems] WHERE [Url] IN (N'/live-airspace', N'/airspace-demo');");
            migrationBuilder.Sql(@"DELETE FROM [MenuItems] WHERE [ParentId] IS NOT NULL AND [Url] = N'/airspace-management' AND [TitleAr] = N'نظرة عامة';");
            migrationBuilder.Sql(@"UPDATE [HeroSlides] SET [SecondaryButtonTextAr] = NULL, [SecondaryButtonTextEn] = NULL, [SecondaryButtonUrl] = NULL
WHERE [SecondaryButtonUrl] IN (N'/live-airspace', N'/airspace-demo');");

            migrationBuilder.DropTable(
                name: "AirspaceVolumes");

            migrationBuilder.DropTable(
                name: "DemoAirports");

            migrationBuilder.DropTable(
                name: "DemoSectors");

            migrationBuilder.DropTable(
                name: "Notams");

            migrationBuilder.DropTable(
                name: "VolumeLoadSamples");

            migrationBuilder.DropColumn(
                name: "DemoDataSeeded",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DemoEnabled",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DemoLeadAr",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DemoLeadEn",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DemoTitleAr",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DemoTitleEn",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DemoTrafficPercent",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FooterNoticeTextAr",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FooterNoticeTextEn",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FooterNoticeUrl",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LiveDataSeeded",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LiveEnabled",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LiveLeadAr",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LiveLeadEn",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LiveTitleAr",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LiveTitleEn",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ShowFooterNotice",
                table: "SiteSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DemoDataSeeded",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DemoEnabled",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DemoLeadAr",
                table: "SiteSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DemoLeadEn",
                table: "SiteSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DemoTitleAr",
                table: "SiteSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DemoTitleEn",
                table: "SiteSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DemoTrafficPercent",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FooterNoticeTextAr",
                table: "SiteSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterNoticeTextEn",
                table: "SiteSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterNoticeUrl",
                table: "SiteSettings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LiveDataSeeded",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LiveEnabled",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LiveLeadAr",
                table: "SiteSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LiveLeadEn",
                table: "SiteSettings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LiveTitleAr",
                table: "SiteSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LiveTitleEn",
                table: "SiteSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowFooterNotice",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AirspaceVolumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoundaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AirspaceVolumes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemoAirports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoAirports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemoSectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    GridColumn = table.Column<int>(type: "int", nullable: false),
                    GridRow = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoSectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Airways = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Fir = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsEstimatedEnd = table.Column<bool>(type: "bit", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    LowerFl = table.Column<int>(type: "int", nullable: true),
                    Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    QCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RadiusNm = table.Column<int>(type: "int", nullable: true),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Schedule = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpperFl = table.Column<int>(type: "int", nullable: true),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VolumeLoadSamples",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HourUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeakCount = table.Column<int>(type: "int", nullable: false),
                    Samples = table.Column<int>(type: "int", nullable: false),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    VolumeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolumeLoadSamples", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AirspaceVolumes_Code",
                table: "AirspaceVolumes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemoAirports_Code",
                table: "DemoAirports",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemoSectors_Code",
                table: "DemoSectors",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notams_ExternalId",
                table: "Notams",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Notams_ValidToUtc",
                table: "Notams",
                column: "ValidToUtc");

            migrationBuilder.CreateIndex(
                name: "IX_VolumeLoadSamples_HourUtc",
                table: "VolumeLoadSamples",
                column: "HourUtc");

            migrationBuilder.CreateIndex(
                name: "IX_VolumeLoadSamples_VolumeCode_HourUtc",
                table: "VolumeLoadSamples",
                columns: new[] { "VolumeCode", "HourUtc" },
                unique: true);
        }
    }
}
