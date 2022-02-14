using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    public partial class CreateParticipations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "participations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    comment = table.Column<string>(type: "text", nullable: true),
                    vod = table.Column<string>(type: "text", nullable: true),
                    runner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participations", x => x.id);
                    table.ForeignKey(
                        name: "fk_participations_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_participations_users_runner_id",
                        column: x => x.runner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_participations_run_id",
                table: "participations",
                column: "run_id");

            migrationBuilder.CreateIndex(
                name: "ix_participations_runner_id",
                table: "participations",
                column: "runner_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "participations");
        }
    }
}
