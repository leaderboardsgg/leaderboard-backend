using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "users",
                type: "character varying(25)",
                maxLength: 25,
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_password_MinLength",
                table: "users",
                sql: "LENGTH(password) >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_username_MinLength",
                table: "users",
                sql: "LENGTH(username) >= 2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_leaderboards_name_MinLength",
                table: "leaderboards",
                sql: "LENGTH(name) >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_categories_name_MinLength",
                table: "categories",
                sql: "LENGTH(name) >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_users_password_MinLength",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_username_MinLength",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_leaderboards_name_MinLength",
                table: "leaderboards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_categories_name_MinLength",
                table: "categories");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "users",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "character varying(25)",
                oldMaxLength: 25,
                oldCollation: "case_insensitive");
        }
    }
}
