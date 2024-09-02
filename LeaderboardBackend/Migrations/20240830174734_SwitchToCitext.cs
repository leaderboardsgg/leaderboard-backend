using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class SwitchToCitext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:run_type", "time,score")
                .Annotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .Annotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:run_type", "time,score")
                .OldAnnotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .OldAnnotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "users",
                type: "citext",
                maxLength: 25,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(25)",
                oldMaxLength: 25,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:run_type", "time,score")
                .Annotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .Annotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned")
                .OldAnnotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:run_type", "time,score")
                .OldAnnotation("Npgsql:Enum:sort_direction", "ascending,descending")
                .OldAnnotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "users",
                type: "character varying(25)",
                maxLength: 25,
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "citext",
                oldMaxLength: 25);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "citext");
        }
    }
}
