using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LeaderboardBackend.Migrations
{
	public partial class CreateTimeMetrics : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "time_metrics",
				columns: table => new
				{
					id = table.Column<long>(type: "bigint", nullable: false)
						.Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
					name = table.Column<string>(type: "text", nullable: false),
					min = table.Column<Period>(type: "interval", nullable: false),
					max = table.Column<Period>(type: "interval", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("pk_time_metrics", x => x.id);
				});
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "time_metrics");
		}
	}
}
