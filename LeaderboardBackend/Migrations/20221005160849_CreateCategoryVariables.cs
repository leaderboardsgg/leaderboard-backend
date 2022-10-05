using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    public partial class CreateCategoryVariables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category_variable",
                columns: table => new
                {
                    categories_id = table.Column<long>(type: "bigint", nullable: false),
                    variables_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_variable", x => new { x.categories_id, x.variables_id });
                    table.ForeignKey(
                        name: "fk_category_variable_categories_categories_id",
                        column: x => x.categories_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_category_variable_variables_variables_id",
                        column: x => x.variables_id,
                        principalTable: "variables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_category_variable_variables_id",
                table: "category_variable",
                column: "variables_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_variable");
        }
    }
}
