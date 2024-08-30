using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCaseInsensitiveCollation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:run_type", "time,score")
                .Annotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .Annotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:run_type", "time,score")
                .OldAnnotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .OldAnnotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:run_type", "time,score")
                .Annotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .Annotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:run_type", "time,score")
                .OldAnnotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .OldAnnotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}
