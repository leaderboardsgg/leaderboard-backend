using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class LeaderboardsExcludeDeletedFromIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_leaderboards_slug",
                table: "leaderboards");

            migrationBuilder.CreateIndex(
                name: "ix_leaderboards_slug",
                table: "leaderboards",
                column: "slug",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_leaderboards_slug",
                table: "leaderboards");

            migrationBuilder.CreateIndex(
                name: "ix_leaderboards_slug",
                table: "leaderboards",
                column: "slug",
                unique: true);
        }
    }
}
