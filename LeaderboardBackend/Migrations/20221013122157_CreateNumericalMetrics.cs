using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    public partial class CreateNumericalMetrics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "numerical_metrics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    min = table.Column<long>(type: "bigint", nullable: false),
                    max = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_numerical_metrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "category_numerical_metric",
                columns: table => new
                {
                    categories_id = table.Column<long>(type: "bigint", nullable: false),
                    numerical_metrics_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_numerical_metric", x => new { x.categories_id, x.numerical_metrics_id });
                    table.ForeignKey(
                        name: "fk_category_numerical_metric_categories_categories_id",
                        column: x => x.categories_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_category_numerical_metric_numerical_metrics_numerical_metri",
                        column: x => x.numerical_metrics_id,
                        principalTable: "numerical_metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "numerical_metric_run",
                columns: table => new
                {
                    numerical_metrics_id = table.Column<long>(type: "bigint", nullable: false),
                    runs_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_numerical_metric_run", x => new { x.numerical_metrics_id, x.runs_id });
                    table.ForeignKey(
                        name: "fk_numerical_metric_run_numerical_metrics_numerical_metrics_id",
                        column: x => x.numerical_metrics_id,
                        principalTable: "numerical_metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_numerical_metric_run_runs_runs_id",
                        column: x => x.runs_id,
                        principalTable: "runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_category_numerical_metric_numerical_metrics_id",
                table: "category_numerical_metric",
                column: "numerical_metrics_id");

            migrationBuilder.CreateIndex(
                name: "ix_numerical_metric_run_runs_id",
                table: "numerical_metric_run",
                column: "runs_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_numerical_metric");

            migrationBuilder.DropTable(
                name: "numerical_metric_run");

            migrationBuilder.DropTable(
                name: "numerical_metrics");
        }
    }
}
