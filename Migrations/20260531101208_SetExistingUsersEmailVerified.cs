using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMailerBackend.Migrations
{
    /// <inheritdoc />
    public partial class SetExistingUsersEmailVerified : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Users"
                SET "EmailVerified" = true
                WHERE "EmailVerificationToken" IS NULL
                  AND "EmailVerified" = false
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
