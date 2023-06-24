using System;
using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class UsersAddRoleAndDropAbout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "about",
                table: "users");

            migrationBuilder.AddColumn<byte>(
                name: "role",
                table: "users",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1); // REGISTERED role

            // migrate "admin" column to roles
            migrationBuilder.UpdateData(
                table: "users",
                keyColumns: new[] { "admin" },
                keyColumnTypes: new[] { "bool" },
                keyValues: new object[] { true },
                columns: new[] { "role" },
                columnTypes: new[] { "" },
                values: new object[] { (byte)3 }); // ADMINISTRATOR role

            migrationBuilder.DropColumn(
                name: "admin",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                keyColumn: "role",
                keyValue: (byte)3, // ADMINISTRATOR role
                column: "admin",
                value: true);

            migrationBuilder.DropColumn(
                name: "role",
                table: "users");
        }
    }
}
