using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class RunAddUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "runs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_runs_user_id",
                table: "runs",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_runs_users_user_id",
                table: "runs",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_runs_users_user_id",
                table: "runs");

            migrationBuilder.DropIndex(
                name: "ix_runs_user_id",
                table: "runs");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "runs");
        }
    }
}
