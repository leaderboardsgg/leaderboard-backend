using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class CategoryAddSortDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .Annotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned")
                .OldAnnotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned");

            migrationBuilder.AddColumn<SortDirection>(
                name: "sort_direction",
                table: "categories",
                type: "sort_direction",
                nullable: false,
                defaultValue: SortDirection.Ascending);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sort_direction",
                table: "categories");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned")
                .OldAnnotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .OldAnnotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned");
        }
    }
}
