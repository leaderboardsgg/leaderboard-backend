using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNonMvpSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bans");

            migrationBuilder.DropTable(
                name: "judgements");

            migrationBuilder.DropTable(
                name: "modships");

            migrationBuilder.DropTable(
                name: "participations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bans",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    banneduserid = table.Column<Guid>(name: "banned_user_id", type: "uuid", nullable: false),
                    banninguserid = table.Column<Guid>(name: "banning_user_id", type: "uuid", nullable: true),
                    leaderboardid = table.Column<long>(name: "leaderboard_id", type: "bigint", nullable: true),
                    createdat = table.Column<Instant>(name: "created_at", type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deletedat = table.Column<Instant>(name: "deleted_at", type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bans", x => x.id);
                    table.ForeignKey(
                        name: "fk_bans_leaderboards_leaderboard_id",
                        column: x => x.leaderboardid,
                        principalTable: "leaderboards",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_bans_users_banned_user_id",
                        column: x => x.banneduserid,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bans_users_banning_user_id",
                        column: x => x.banninguserid,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "judgements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    judgeid = table.Column<Guid>(name: "judge_id", type: "uuid", nullable: false),
                    runid = table.Column<Guid>(name: "run_id", type: "uuid", nullable: false),
                    approved = table.Column<bool>(type: "boolean", nullable: true),
                    createdat = table.Column<Instant>(name: "created_at", type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    note = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_judgements", x => x.id);
                    table.ForeignKey(
                        name: "fk_judgements_runs_run_id",
                        column: x => x.runid,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_judgements_users_judge_id",
                        column: x => x.judgeid,
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
                    leaderboardid = table.Column<long>(name: "leaderboard_id", type: "bigint", nullable: false),
                    userid = table.Column<Guid>(name: "user_id", type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modships", x => x.id);
                    table.ForeignKey(
                        name: "fk_modships_leaderboards_leaderboard_id",
                        column: x => x.leaderboardid,
                        principalTable: "leaderboards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_modships_users_user_id",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "participations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    runid = table.Column<Guid>(name: "run_id", type: "uuid", nullable: false),
                    runnerid = table.Column<Guid>(name: "runner_id", type: "uuid", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    vod = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participations", x => x.id);
                    table.ForeignKey(
                        name: "fk_participations_runs_run_id",
                        column: x => x.runid,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_participations_users_runner_id",
                        column: x => x.runnerid,
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
                name: "ix_judgements_judge_id",
                table: "judgements",
                column: "judge_id");

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

            migrationBuilder.CreateIndex(
                name: "ix_participations_run_id",
                table: "participations",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "ix_participations_runner_id",
                table: "participations",
                column: "runner_id");
        }
    }
}
