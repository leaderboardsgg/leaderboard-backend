using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeaderboardBackend.Migrations
{
	public partial class InitMigration : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "leaderboards",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					name = table.Column<string>(type: "text", nullable: false),
					slug = table.Column<string>(type: "text", nullable: false),
					rules = table.Column<string>(type: "text", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("pk_leaderboards", x => x.id);
				});

			migrationBuilder.CreateTable(
				name: "runs",
				columns: table => new
				{
					id = table.Column<Guid>(type: "uuid", nullable: false),
					played = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					submitted = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					status = table.Column<int>(type: "integer", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("pk_runs", x => x.id);
				});

			migrationBuilder.CreateTable(
				name: "users",
				columns: table => new
				{
					id = table.Column<Guid>(type: "uuid", nullable: false),
					username = table.Column<string>(type: "text", nullable: false),
					email = table.Column<string>(type: "text", nullable: false),
					password = table.Column<string>(type: "text", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("pk_users", x => x.id);
				});

			migrationBuilder.CreateTable(
				name: "categories",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					name = table.Column<string>(type: "text", nullable: false),
					slug = table.Column<string>(type: "text", nullable: false),
					rules = table.Column<string>(type: "text", nullable: true),
					players_min = table.Column<int>(type: "integer", nullable: false),
					players_max = table.Column<int>(type: "integer", nullable: false),
					leaderboard_id = table.Column<long>(type: "bigint", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("pk_categories", x => x.id);
					table.ForeignKey(
						name: "fk_categories_leaderboards_leaderboard_id",
						column: x => x.leaderboard_id,
						principalTable: "leaderboards",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "bans",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					reason = table.Column<string>(type: "text", nullable: false),
					time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					banning_user_id = table.Column<Guid>(type: "uuid", nullable: true),
					banned_user_id = table.Column<Guid>(type: "uuid", nullable: false),
					leaderboard_id = table.Column<long>(type: "bigint", nullable: true)
				},
				constraints: table =>
				{
					table.PrimaryKey("pk_bans", x => x.id);
					table.ForeignKey(
						name: "fk_bans_leaderboards_leaderboard_id",
						column: x => x.leaderboard_id,
						principalTable: "leaderboards",
						principalColumn: "id");
					table.ForeignKey(
						name: "fk_bans_users_banned_user_id",
						column: x => x.banned_user_id,
						principalTable: "users",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "fk_bans_users_banning_user_id",
						column: x => x.banning_user_id,
						principalTable: "users",
						principalColumn: "id");
				});

			migrationBuilder.CreateTable(
				name: "judgements",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					approved = table.Column<bool>(type: "boolean", nullable: true),
					timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
					note = table.Column<string>(type: "text", nullable: false),
					approver_id = table.Column<Guid>(type: "uuid", nullable: false),
					run_id = table.Column<Guid>(type: "uuid", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("pk_judgements", x => x.id);
					table.ForeignKey(
						name: "fk_judgements_runs_run_id",
						column: x => x.run_id,
						principalTable: "runs",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "fk_judgements_users_approver_id",
						column: x => x.approver_id,
						principalTable: "users",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateTable(
				name: "modships",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					user_id = table.Column<Guid>(type: "uuid", nullable: false),
					leaderboard_id = table.Column<long>(type: "bigint", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("pk_modships", x => x.id);
					table.ForeignKey(
						name: "fk_modships_leaderboards_leaderboard_id",
						column: x => x.leaderboard_id,
						principalTable: "leaderboards",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "fk_modships_users_user_id",
						column: x => x.user_id,
						principalTable: "users",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "ix_bans_banned_user_id",
				table: "bans",
				column: "banned_user_id");

			migrationBuilder.CreateIndex(
				name: "ix_bans_banning_user_id",
				table: "bans",
				column: "banning_user_id");

			migrationBuilder.CreateIndex(
				name: "ix_bans_leaderboard_id",
				table: "bans",
				column: "leaderboard_id");

			migrationBuilder.CreateIndex(
				name: "ix_categories_leaderboard_id",
				table: "categories",
				column: "leaderboard_id");

			migrationBuilder.CreateIndex(
				name: "ix_judgements_approver_id",
				table: "judgements",
				column: "approver_id");

			migrationBuilder.CreateIndex(
				name: "ix_judgements_run_id",
				table: "judgements",
				column: "run_id");

			migrationBuilder.CreateIndex(
				name: "ix_modships_leaderboard_id",
				table: "modships",
				column: "leaderboard_id");

			migrationBuilder.CreateIndex(
				name: "ix_modships_user_id",
				table: "modships",
				column: "user_id");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "bans");

			migrationBuilder.DropTable(
				name: "categories");

			migrationBuilder.DropTable(
				name: "judgements");

			migrationBuilder.DropTable(
				name: "modships");

			migrationBuilder.DropTable(
				name: "runs");

			migrationBuilder.DropTable(
				name: "leaderboards");

			migrationBuilder.DropTable(
				name: "users");
		}
	}
}
