using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class RunTrackChangesAndDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "submitted_at",
                table: "runs",
                newName: "created_at");

            migrationBuilder.AddColumn<Instant>(
                name: "deleted_at",
                table: "runs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Instant>(
                name: "updated_at",
                table: "runs",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "runs");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "runs",
                newName: "submitted_at");
        }
    }
}
