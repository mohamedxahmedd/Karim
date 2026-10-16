using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NationalAirspace.Infrastructure.Persistence.Migrations
{
    /// <summary>Content icons move from Bootstrap Icons ("bi-…") to Phosphor Light ("ph-…").</summary>
    public partial class PhosphorIcons : Migration
    {
        private static readonly string[] Tables = ["CenterServices", "Statistics", "ImportantLinks", "PageSections"];

        private static readonly Dictionary<string, string> Map = new()
        {
            ["bi-airplane"] = "ph-airplane-tilt",
            ["bi-archive"] = "ph-archive",
            ["bi-arrow-left-right"] = "ph-path",
            ["bi-arrow-repeat"] = "ph-arrows-clockwise",
            ["bi-arrow-up"] = "ph-arrow-up",
            ["bi-award"] = "ph-medal",
            ["bi-bank"] = "ph-bank",
            ["bi-bar-chart"] = "ph-chart-bar",
            ["bi-bell"] = "ph-bell",
            ["bi-bounding-box"] = "ph-polygon",
            ["bi-bounding-box-circles"] = "ph-intersect",
            ["bi-box-arrow-up-right"] = "ph-arrow-square-out",
            ["bi-building"] = "ph-buildings",
            ["bi-bullseye"] = "ph-target",
            ["bi-calendar-event"] = "ph-calendar-check",
            ["bi-calendar3"] = "ph-calendar-blank",
            ["bi-chat-dots"] = "ph-chat-circle-dots",
            ["bi-chat-square-text"] = "ph-chat-centered-text",
            ["bi-check-circle-fill"] = "ph-check-circle",
            ["bi-check2-circle"] = "ph-check-circle",
            ["bi-chevron-left"] = "ph-caret-left",
            ["bi-chevron-right"] = "ph-caret-right",
            ["bi-clipboard-data"] = "ph-clipboard-text",
            ["bi-clock"] = "ph-clock",
            ["bi-clock-history"] = "ph-clock-counter-clockwise",
            ["bi-compass"] = "ph-compass",
            ["bi-cone-striped"] = "ph-traffic-cone",
            ["bi-database"] = "ph-database",
            ["bi-diagram-3"] = "ph-tree-structure",
            ["bi-display"] = "ph-monitor",
            ["bi-download"] = "ph-download-simple",
            ["bi-envelope"] = "ph-envelope-simple",
            ["bi-exclamation-diamond"] = "ph-warning-diamond",
            ["bi-exclamation-octagon"] = "ph-warning-octagon",
            ["bi-exclamation-octagon-fill"] = "ph-warning-octagon",
            ["bi-exclamation-triangle"] = "ph-warning",
            ["bi-exclamation-triangle-fill"] = "ph-warning",
            ["bi-eye"] = "ph-eye",
            ["bi-facebook"] = "ph-facebook-logo",
            ["bi-file-earmark"] = "ph-file",
            ["bi-file-earmark-pdf"] = "ph-file-pdf",
            ["bi-file-earmark-text"] = "ph-file-text",
            ["bi-flag"] = "ph-flag",
            ["bi-gear"] = "ph-gear-six",
            ["bi-geo-alt"] = "ph-map-pin",
            ["bi-globe"] = "ph-globe",
            ["bi-globe2"] = "ph-globe-hemisphere-east",
            ["bi-graph-up"] = "ph-chart-line-up",
            ["bi-graph-up-arrow"] = "ph-trend-up",
            ["bi-grid"] = "ph-squares-four",
            ["bi-hash"] = "ph-hash",
            ["bi-hourglass-split"] = "ph-hourglass-medium",
            ["bi-house"] = "ph-house",
            ["bi-images"] = "ph-images",
            ["bi-inbox"] = "ph-tray",
            ["bi-info-circle"] = "ph-info",
            ["bi-instagram"] = "ph-instagram-logo",
            ["bi-journal-text"] = "ph-book-open-text",
            ["bi-laptop"] = "ph-laptop",
            ["bi-lightning-charge"] = "ph-lightning",
            ["bi-link-45deg"] = "ph-link-simple",
            ["bi-linkedin"] = "ph-linkedin-logo",
            ["bi-list"] = "ph-list",
            ["bi-map"] = "ph-map-trifold",
            ["bi-megaphone"] = "ph-megaphone",
            ["bi-mortarboard"] = "ph-graduation-cap",
            ["bi-newspaper"] = "ph-newspaper",
            ["bi-paperclip"] = "ph-paperclip",
            ["bi-pencil-square"] = "ph-pencil-simple-line",
            ["bi-people"] = "ph-users-three",
            ["bi-person"] = "ph-user",
            ["bi-printer"] = "ph-printer",
            ["bi-question-circle"] = "ph-question",
            ["bi-search"] = "ph-magnifying-glass",
            ["bi-send"] = "ph-paper-plane-tilt",
            ["bi-shield-check"] = "ph-shield-check",
            ["bi-shield-lock"] = "ph-lock-simple",
            ["bi-shuffle"] = "ph-shuffle",
            ["bi-sliders"] = "ph-sliders-horizontal",
            ["bi-speedometer2"] = "ph-gauge",
            ["bi-telephone"] = "ph-phone",
            ["bi-translate"] = "ph-translate",
            ["bi-twitter-x"] = "ph-x-logo",
            ["bi-youtube"] = "ph-youtube-logo",
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => Convert(migrationBuilder, Map, "bi-");

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            Convert(migrationBuilder, Map.GroupBy(p => p.Value).ToDictionary(g => g.Key, g => g.First().Key), "ph-");

        /// <summary>Rewrites known icon names; unknown names with the old prefix are cleared so the default icon shows.</summary>
        private static void Convert(MigrationBuilder migrationBuilder, IReadOnlyDictionary<string, string> map, string oldPrefix)
        {
            var cases = string.Join(" ", map.Select(p => $"WHEN '{p.Key}' THEN '{p.Value}'"));
            // Brackets matter: "Statistics" is a reserved word in SQL Server.
            foreach (var table in Tables)
                migrationBuilder.Sql($"UPDATE [{table}] SET Icon = CASE Icon {cases} ELSE NULL END WHERE Icon LIKE '{oldPrefix}%';");
        }
    }
}
