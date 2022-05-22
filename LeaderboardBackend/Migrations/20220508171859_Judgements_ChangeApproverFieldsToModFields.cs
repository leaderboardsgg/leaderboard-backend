using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
	public partial class Judgements_ChangeApproverFieldsToModFields : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "fk_judgements_users_approver_id",
				table: "judgements");

			migrationBuilder.RenameColumn(
				name: "approver_id",
				table: "judgements",
				newName: "mod_id");

			migrationBuilder.RenameIndex(
				name: "ix_judgements_approver_id",
				table: "judgements",
				newName: "ix_judgements_mod_id");

			migrationBuilder.AddForeignKey(
				name: "fk_judgements_users_mod_id",
				table: "judgements",
				column: "mod_id",
				principalTable: "users",
				principalColumn: "id",
				onDelete: ReferentialAction.Cascade);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "fk_judgements_users_mod_id",
				table: "judgements");

			migrationBuilder.RenameColumn(
				name: "mod_id",
				table: "judgements",
				newName: "approver_id");

			migrationBuilder.RenameIndex(
				name: "ix_judgements_mod_id",
				table: "judgements",
				newName: "ix_judgements_approver_id");

			migrationBuilder.AddForeignKey(
				name: "fk_judgements_users_approver_id",
				table: "judgements",
				column: "approver_id",
				principalTable: "users",
				principalColumn: "id",
				onDelete: ReferentialAction.Cascade);
		}
	}
}
