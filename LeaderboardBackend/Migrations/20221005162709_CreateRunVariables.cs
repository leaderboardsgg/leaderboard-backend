using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeaderboardBackend.Migrations
{
	public partial class CreateRunVariables : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "variable_values",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					name = table.Column<string>(type: "text", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("pk_variable_values", x => x.id);
				});

			migrationBuilder.CreateTable(
				name: "run_variable_value",
				columns: table => new
				{
					runs_id = table.Column<Guid>(type: "uuid", nullable: false),
					variable_values_id = table.Column<long>(type: "bigint", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("pk_run_variable_value", x => new { x.runs_id, x.variable_values_id });
					table.ForeignKey(
						name: "fk_run_variable_value_runs_runs_id",
						column: x => x.runs_id,
						principalTable: "runs",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
					table.ForeignKey(
						name: "fk_run_variable_value_variable_values_variable_values_id",
						column: x => x.variable_values_id,
						principalTable: "variable_values",
						principalColumn: "id",
						onDelete: ReferentialAction.Cascade);
				});

			migrationBuilder.CreateIndex(
				name: "ix_run_variable_value_variable_values_id",
				table: "run_variable_value",
				column: "variable_values_id");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "run_variable_value");

			migrationBuilder.DropTable(
				name: "variable_values");
		}
	}
}
