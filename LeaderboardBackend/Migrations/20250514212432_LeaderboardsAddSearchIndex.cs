using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class LeaderboardsAddSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_leaderboards_name",
                table: "leaderboards",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "english");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_leaderboards_name",
                table: "leaderboards");
        }
    }
}
