using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NationalAirspace.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FooterSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FooterContactTitleAr",
                table: "SiteSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterContactTitleEn",
                table: "SiteSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterCopyrightAr",
                table: "SiteSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterCopyrightEn",
                table: "SiteSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterLinksTitleAr",
                table: "SiteSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterLinksTitleEn",
                table: "SiteSettings",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

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
                name: "ShowFooterNotice",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: true); // keep the existing demo-notice link visible on live sites
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FooterContactTitleAr",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FooterContactTitleEn",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FooterCopyrightAr",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FooterCopyrightEn",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FooterLinksTitleAr",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FooterLinksTitleEn",
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
                name: "ShowFooterNotice",
                table: "SiteSettings");
        }
    }
}
