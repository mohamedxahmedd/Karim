using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NationalAirspace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LiveAirspaceMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                defaultValue: true);

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

            migrationBuilder.CreateTable(
                name: "AirspaceVolumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    BoundaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AirspaceVolumes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Fir = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    QCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    RadiusNm = table.Column<int>(type: "int", nullable: true),
                    LowerFl = table.Column<int>(type: "int", nullable: true),
                    UpperFl = table.Column<int>(type: "int", nullable: true),
                    ValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsEstimatedEnd = table.Column<bool>(type: "bit", nullable: false),
                    Schedule = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Airways = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    VolumeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HourUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeakCount = table.Column<int>(type: "int", nullable: false),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    Samples = table.Column<int>(type: "int", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AirspaceVolumes");

            migrationBuilder.DropTable(
                name: "Notams");

            migrationBuilder.DropTable(
                name: "VolumeLoadSamples");

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
        }
    }
}
