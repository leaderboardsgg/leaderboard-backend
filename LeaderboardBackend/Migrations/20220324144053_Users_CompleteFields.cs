using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
	public partial class Users_CompleteFields : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<string>(
				name: "about",
				table: "users",
				type: "text",
				nullable: true);

			migrationBuilder.AddColumn<bool>(
				name: "admin",
				table: "users",
				type: "boolean",
				nullable: false,
				defaultValue: false);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "about",
				table: "users");

			migrationBuilder.DropColumn(
				name: "admin",
				table: "users");
		}
	}
}
