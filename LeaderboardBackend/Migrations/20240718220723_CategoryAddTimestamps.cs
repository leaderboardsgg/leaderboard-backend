using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class CategoryAddTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Instant>(
                name: "created_at",
                table: "categories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));

            migrationBuilder.AddColumn<Instant>(
                name: "deleted_at",
                table: "categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Instant>(
                name: "updated_at",
                table: "categories",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "categories");
        }
    }
}
