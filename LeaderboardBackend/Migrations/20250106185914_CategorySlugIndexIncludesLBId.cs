using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class CategorySlugIndexIncludesLBId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_categories_leaderboard_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "ix_categories_slug",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "ix_categories_leaderboard_id_slug",
                table: "categories",
                columns: new[] { "leaderboard_id", "slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_categories_leaderboard_id_slug",
                table: "categories");

            migrationBuilder.CreateIndex(
                name: "ix_categories_leaderboard_id",
                table: "categories",
                column: "leaderboard_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_slug",
                table: "categories",
                column: "slug",
                unique: true);
        }
    }
}
