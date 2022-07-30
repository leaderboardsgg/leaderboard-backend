using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    public partial class Run_PlayedOn_InstantToLocalDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_judgements_users_mod_id",
                table: "judgements");

            migrationBuilder.DropColumn(
                name: "played",
                table: "runs");

            migrationBuilder.RenameColumn(
                name: "submitted",
                table: "runs",
                newName: "submitted_at");

            migrationBuilder.RenameColumn(
                name: "mod_id",
                table: "judgements",
                newName: "judge_id");

            migrationBuilder.RenameIndex(
                name: "ix_judgements_mod_id",
                table: "judgements",
                newName: "ix_judgements_judge_id");

            migrationBuilder.AddColumn<LocalDate>(
                name: "played_on",
                table: "runs",
                type: "date",
                nullable: false,
                defaultValue: new NodaTime.LocalDate(1, 1, 1));

            migrationBuilder.AddForeignKey(
                name: "fk_judgements_users_judge_id",
                table: "judgements",
                column: "judge_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_judgements_users_judge_id",
                table: "judgements");

            migrationBuilder.DropColumn(
                name: "played_on",
                table: "runs");

            migrationBuilder.RenameColumn(
                name: "submitted_at",
                table: "runs",
                newName: "submitted");

            migrationBuilder.RenameColumn(
                name: "judge_id",
                table: "judgements",
                newName: "mod_id");

            migrationBuilder.RenameIndex(
                name: "ix_judgements_judge_id",
                table: "judgements",
                newName: "ix_judgements_mod_id");

            migrationBuilder.AddColumn<Instant>(
                name: "played",
                table: "runs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));

            migrationBuilder.AddForeignKey(
                name: "fk_judgements_users_mod_id",
                table: "judgements",
                column: "mod_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
