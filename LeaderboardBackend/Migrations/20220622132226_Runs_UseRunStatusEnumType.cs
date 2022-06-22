using LeaderboardBackend.Models.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    public partial class Runs_UseRunStatusEnumType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:metric_type", "int,float,interval")
                .Annotation("Npgsql:Enum:run_status", "created,submitted,pending,approved,rejected")
                .OldAnnotation("Npgsql:Enum:metric_type", "int,float,interval");

			// Credits: https://w.wol.ph/2020/03/18/altering-postgres-int-columns-to-enum-type/
			migrationBuilder.Sql(
				@"ALTER TABLE runs
ALTER status
TYPE run_status
USING (enum_range(null::run_status))[status::int];");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
			// Credits: https://stackoverflow.com/a/16044843
			migrationBuilder.Sql(
				@"ALTER TABLE runs
ALTER status
TYPE integer
USING array_length(enum_range(NULL, status::run_status), 1);");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:metric_type", "int,float,interval")
                .OldAnnotation("Npgsql:Enum:metric_type", "int,float,interval")
                .OldAnnotation("Npgsql:Enum:run_status", "created,submitted,pending,approved,rejected");
        }
    }
}
