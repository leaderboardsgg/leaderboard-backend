using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    public partial class Bans_AddDeletedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "bans",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "bans");
        }
    }
}
