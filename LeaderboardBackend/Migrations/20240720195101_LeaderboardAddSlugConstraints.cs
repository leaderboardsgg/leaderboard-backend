using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class LeaderboardAddSlugConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "leaderboards",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddCheckConstraint(
                name: "CK_leaderboards_slug_MinLength",
                table: "leaderboards",
                sql: "LENGTH(slug) >= 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_leaderboards_slug_RegularExpression",
                table: "leaderboards",
                sql: "slug ~ '^([a-zA-Z0-9-_]|%[A-F0-9]{2})*$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_leaderboards_slug_MinLength",
                table: "leaderboards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_leaderboards_slug_RegularExpression",
                table: "leaderboards");

            migrationBuilder.AlterColumn<string>(
                name: "slug",
                table: "leaderboards",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);
        }
    }
}
