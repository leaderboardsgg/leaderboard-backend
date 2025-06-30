using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class LeaderboardsNoNumericSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_leaderboards_slug_RegularExpression",
                table: "leaderboards");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:run_type", "score,time")
                .Annotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .Annotation("Npgsql:Enum:user_role", "administrator,banned,confirmed,registered")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:run_type", "time,score")
                .OldAnnotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .OldAnnotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AddCheckConstraint(
                name: "CK_leaderboards_slug_RegularExpression",
                table: "leaderboards",
                sql: "slug ~ '^(?!([0-9]+)$)[a-zA-Z0-9\\-_]*$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_leaderboards_slug_RegularExpression",
                table: "leaderboards");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:run_type", "time,score")
                .Annotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .Annotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:run_type", "score,time")
                .OldAnnotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .OldAnnotation("Npgsql:Enum:user_role", "administrator,banned,confirmed,registered")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AddCheckConstraint(
                name: "CK_leaderboards_slug_RegularExpression",
                table: "leaderboards",
                sql: "slug ~ '^[a-zA-Z0-9\\-_]*$'");
        }
    }
}
