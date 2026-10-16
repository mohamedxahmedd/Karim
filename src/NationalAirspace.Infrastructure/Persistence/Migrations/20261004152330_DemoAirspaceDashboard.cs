using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NationalAirspace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DemoAirspaceDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                defaultValue: true); // keep the demo page live after upgrading

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
                defaultValue: 100); // normal traffic

            migrationBuilder.CreateTable(
                name: "DemoAirports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GridColumn = table.Column<int>(type: "int", nullable: false),
                    GridRow = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoSectors", x => x.Id);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoAirports");

            migrationBuilder.DropTable(
                name: "DemoSectors");

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
        }
    }
}
