using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class NoUrlEncodingInSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_leaderboards_slug_RegularExpression",
                table: "leaderboards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_categories_slug_RegularExpression",
                table: "categories");

            migrationBuilder.AddCheckConstraint(
                name: "CK_leaderboards_slug_RegularExpression",
                table: "leaderboards",
                sql: "slug ~ '^[a-zA-Z0-9\\-_]*$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_categories_slug_RegularExpression",
                table: "categories",
                sql: "slug ~ '^[a-zA-Z0-9\\-_]*$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_leaderboards_slug_RegularExpression",
                table: "leaderboards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_categories_slug_RegularExpression",
                table: "categories");

            migrationBuilder.AddCheckConstraint(
                name: "CK_leaderboards_slug_RegularExpression",
                table: "leaderboards",
                sql: "slug ~ '^([a-zA-Z0-9\\-_]|%[A-F0-9]{2})*$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_categories_slug_RegularExpression",
                table: "categories",
                sql: "slug ~ '^([a-zA-Z0-9\\-_]|%[A-F0-9]{2})*$'");
        }
    }
}
