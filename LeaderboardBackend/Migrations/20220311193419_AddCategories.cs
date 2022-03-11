using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    public partial class AddCategories : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    rules = table.Column<string>(type: "text", nullable: false),
                    players_min = table.Column<int>(type: "integer", nullable: false),
                    players_max = table.Column<int>(type: "integer", nullable: false),
                    leaderboard_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_leaderboards_leaderboard_id",
                        column: x => x.leaderboard_id,
                        principalTable: "leaderboards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_leaderboard_id",
                table: "categories",
                column: "leaderboard_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
