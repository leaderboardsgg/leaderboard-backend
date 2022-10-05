using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
	public partial class Run_UseEnumTypeForRunStatus : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterDatabase()
				.Annotation("Npgsql:Enum:run_status", "created,submitted,pending,approved,rejected");

			// Custom SQL to convert int to enum, as EF Core can't do it automatically.
			// https://w.wol.ph/2020/03/18/altering-postgres-int-columns-to-enum-type/
			migrationBuilder.Sql(
				@"ALTER TABLE runs
				ALTER COLUMN status
				TYPE run_status
				USING (enum_range(null::run_status))[status::int + 1];"
			);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// Custom SQL to revert, as EF Core can't do it automatically.
			migrationBuilder.Sql(
				@"ALTER TABLE runs
				ALTER status
				TYPE integer
				USING array_length(enum_range(NULL, status::run_status), 1) - 1;"
			);

			migrationBuilder.AlterDatabase()
				.OldAnnotation("Npgsql:Enum:run_status", "created,submitted,pending,approved,rejected");
		}
	}
}
