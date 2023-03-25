using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
	/// <inheritdoc />
	public partial class UsersSeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			string email = Environment.GetEnvironmentVariable("LGG_ADMIN_EMAIL") ?? "omega@star.com";
			string username = Environment.GetEnvironmentVariable("LGG_ADMIN_USERNAME") ?? "Galactus";
			string password = Environment.GetEnvironmentVariable("LGG_ADMIN_PASSWORD") ?? "3ntr0pyChaos";
			string hashedPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(password);

			migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "about", "admin", "email", "password", "username" },
                values: new object[] { new Guid("421bb896-1990-48c6-8b0c-d69f56d6746a"), null, true, email, hashedPassword, username });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("421bb896-1990-48c6-8b0c-d69f56d6746a"));
        }
    }
}
