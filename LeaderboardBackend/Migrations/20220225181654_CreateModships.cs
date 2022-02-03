using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    public partial class CreateModships : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "modships",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leaderboard_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
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
                name: "modships");
        }
    }
}
