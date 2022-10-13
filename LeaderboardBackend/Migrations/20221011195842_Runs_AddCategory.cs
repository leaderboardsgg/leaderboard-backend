using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
	public partial class Runs_AddCategory : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<long>(
				name: "category_id",
				table: "runs",
				type: "bigint",
				nullable: false,
				defaultValue: 0L);

			// if someone already created runs on their local database, set a category_id, otherwise the migration will fail
			migrationBuilder.Sql("UPDATE runs SET category_id = (SELECT Id FROM categories limit 1)");

			migrationBuilder.CreateIndex(
				name: "ix_runs_category_id",
				table: "runs",
				column: "category_id");

			migrationBuilder.AddForeignKey(
				name: "fk_runs_categories_category_id",
				table: "runs",
				column: "category_id",
				principalTable: "categories",
				principalColumn: "id",
				onDelete: ReferentialAction.Cascade);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "fk_runs_categories_category_id",
				table: "runs");

			migrationBuilder.DropIndex(
				name: "ix_runs_category_id",
				table: "runs");

			migrationBuilder.DropColumn(
				name: "category_id",
				table: "runs");
		}
	}
}
