using System;
using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class Users_UsernameAndEmailCaseInsensitivity_AddRole_DropAbout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "about",
                table: "users");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .Annotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "users",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<UserRole>(
                name: "role",
                table: "users",
                type: "user_role",
                nullable: false,
                defaultValue: UserRole.Registered);

            // migrate "admin" column to roles
            migrationBuilder.UpdateData(
                table: "users",
                keyColumns: new[] { "admin" },
                keyColumnTypes: new[] { "bool" },
                keyValues: new object[] { true },
                columns: new[] { "role" },
                columnTypes: new[] { "user_role" },
                values: new object[] { UserRole.Administrator });

            migrationBuilder.DropColumn(
                name: "admin",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:CollationDefinition:case_insensitive", "und-u-ks-level2,und-u-ks-level2,icu,False")
                .OldAnnotation("Npgsql:Enum:user_role", "registered,confirmed,administrator,banned");

            migrationBuilder.AlterColumn<string>(
                name: "username",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AddColumn<string>(
                name: "about",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "admin",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumns: new[] { "role" },
                keyColumnTypes: new[] { "user_role" },
                keyValues: new object[] { UserRole.Administrator },
                columns: new[] { "admin" },
                columnTypes: new[] { "bool" },
                values: new object[] { true });

            migrationBuilder.DropColumn(
                name: "role",
                table: "users");
        }
    }
}
