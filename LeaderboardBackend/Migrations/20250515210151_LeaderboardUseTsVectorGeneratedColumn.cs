using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class LeaderboardUseTsVectorGeneratedColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_leaderboards_name",
                table: "leaderboards");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "leaderboards",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "name", "slug" });

            migrationBuilder.CreateIndex(
                name: "ix_leaderboards_search_vector",
                table: "leaderboards",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_leaderboards_search_vector",
                table: "leaderboards");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "leaderboards");

            migrationBuilder.CreateIndex(
                name: "ix_leaderboards_name",
                table: "leaderboards",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "english");
        }
    }
}
