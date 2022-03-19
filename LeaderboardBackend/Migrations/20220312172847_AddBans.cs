using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    public partial class AddBans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bans",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    banning_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    banned_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leaderboard_id = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bans");
        }
    }
}
