using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    public partial class Bans_TimestampChanges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.RenameColumn(
				name: "time",
				table: "bans",
				newName: "created_at");

			migrationBuilder.AlterColumn<Instant>(
				name: "created_at",
				table: "bans",
				type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<Instant>(
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

			migrationBuilder.AlterColumn<DateTime>(
				name: "created_at",
				table: "bans",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()",
				oldClrType: typeof(Instant),
				oldType: "timestamp with time zone");

			migrationBuilder.RenameColumn(
				name: "created_at",
				table: "bans",
				newName: "time");
        }
    }
}
