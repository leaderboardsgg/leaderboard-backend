using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    public partial class Bans_ChangeTimeToCreatedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "time",
                table: "bans",
                newName: "created_at");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "bans",
                newName: "time");
        }
    }
}
