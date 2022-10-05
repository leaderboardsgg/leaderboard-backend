using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace LeaderboardBackend.Migrations
{
	public partial class AddTimestampFields : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<Instant>(
				name: "created_at",
				table: "users",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<Instant>(
				name: "deleted_at",
				table: "users",
				type: "timestamp with time zone",
				nullable: true);

			migrationBuilder.AddColumn<Instant>(
				name: "updated_at",
				table: "users",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<Instant>(
				name: "created_at",
				table: "runs",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<Instant>(
				name: "deleted_at",
				table: "runs",
				type: "timestamp with time zone",
				nullable: true);

			migrationBuilder.AddColumn<Instant>(
				name: "updated_at",
				table: "runs",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<Instant>(
				name: "created_at",
				table: "participations",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<Instant>(
				name: "deleted_at",
				table: "participations",
				type: "timestamp with time zone",
				nullable: true);

			migrationBuilder.AddColumn<Instant>(
				name: "updated_at",
				table: "participations",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<Instant>(
				name: "created_at",
				table: "modships",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<Instant>(
				name: "deleted_at",
				table: "modships",
				type: "timestamp with time zone",
				nullable: true);

			migrationBuilder.AddColumn<Instant>(
				name: "updated_at",
				table: "modships",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<Instant>(
				name: "created_at",
				table: "leaderboards",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<Instant>(
				name: "deleted_at",
				table: "leaderboards",
				type: "timestamp with time zone",
				nullable: true);

			migrationBuilder.AddColumn<Instant>(
				name: "updated_at",
				table: "leaderboards",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<Instant>(
				name: "deleted_at",
				table: "judgements",
				type: "timestamp with time zone",
				nullable: true);

			migrationBuilder.AddColumn<Instant>(
				name: "updated_at",
				table: "judgements",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<Instant>(
				name: "created_at",
				table: "categories",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<Instant>(
				name: "deleted_at",
				table: "categories",
				type: "timestamp with time zone",
				nullable: true);

			migrationBuilder.AddColumn<Instant>(
				name: "updated_at",
				table: "categories",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");

			migrationBuilder.AddColumn<Instant>(
				name: "updated_at",
				table: "bans",
				type: "timestamp with time zone",
				nullable: false,
				defaultValueSql: "now()");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "created_at",
				table: "users");

			migrationBuilder.DropColumn(
				name: "deleted_at",
				table: "users");

			migrationBuilder.DropColumn(
				name: "updated_at",
				table: "users");

			migrationBuilder.DropColumn(
				name: "created_at",
				table: "runs");

			migrationBuilder.DropColumn(
				name: "deleted_at",
				table: "runs");

			migrationBuilder.DropColumn(
				name: "updated_at",
				table: "runs");

			migrationBuilder.DropColumn(
				name: "created_at",
				table: "participations");

			migrationBuilder.DropColumn(
				name: "deleted_at",
				table: "participations");

			migrationBuilder.DropColumn(
				name: "updated_at",
				table: "participations");

			migrationBuilder.DropColumn(
				name: "created_at",
				table: "modships");

			migrationBuilder.DropColumn(
				name: "deleted_at",
				table: "modships");

			migrationBuilder.DropColumn(
				name: "updated_at",
				table: "modships");

			migrationBuilder.DropColumn(
				name: "created_at",
				table: "leaderboards");

			migrationBuilder.DropColumn(
				name: "deleted_at",
				table: "leaderboards");

			migrationBuilder.DropColumn(
				name: "updated_at",
				table: "leaderboards");

			migrationBuilder.DropColumn(
				name: "deleted_at",
				table: "judgements");

			migrationBuilder.DropColumn(
				name: "updated_at",
				table: "judgements");

			migrationBuilder.DropColumn(
				name: "created_at",
				table: "categories");

			migrationBuilder.DropColumn(
				name: "deleted_at",
				table: "categories");

			migrationBuilder.DropColumn(
				name: "updated_at",
				table: "categories");

			migrationBuilder.DropColumn(
				name: "updated_at",
				table: "bans");
		}
	}
}
