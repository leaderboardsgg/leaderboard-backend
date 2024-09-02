using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LeaderboardBackend.Migrations
{
    /// <inheritdoc />
    public partial class UserAddUsernameRegex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_users_username_RegularExpression",
                table: "users",
                sql: "username ~ '^[a-zA-Z0-9]([-_'']?[a-zA-Z0-9])+$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_users_username_RegularExpression",
                table: "users");
        }
    }
}
