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
                .OldAnnotation("Npgsql:Enum:type", "int,float,interval");

            migrationBuilder.AlterColumn<RunStatus>(
                name: "status",
                table: "runs",
                type: "run_status",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<MetricType>(
                name: "type",
                table: "metrics",
                type: "metric_type",
                nullable: false,
                oldClrType: typeof(MetricType),
                oldType: "type");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:type", "int,float,interval")
                .OldAnnotation("Npgsql:Enum:metric_type", "int,float,interval")
                .OldAnnotation("Npgsql:Enum:run_status", "created,submitted,pending,approved,rejected");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "runs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(RunStatus),
                oldType: "run_status");

            migrationBuilder.AlterColumn<MetricType>(
                name: "type",
                table: "metrics",
                type: "type",
                nullable: false,
                oldClrType: typeof(MetricType),
                oldType: "metric_type");
        }
    }
}
