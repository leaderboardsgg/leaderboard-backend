using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class CategoryExcludeDeletedFromSlugIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_categories_leaderboard_id_slug",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "ix_categories_leaderboard_id_slug",
                table: "categories",
                columns: new[] { "leaderboard_id", "slug" },
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_categories_leaderboard_id_slug",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "ix_categories_leaderboard_id_slug",
                table: "categories",
                columns: new[] { "leaderboard_id", "slug" },
                unique: true);
        }
    }
}
