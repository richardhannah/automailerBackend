using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMailerBackend.Migrations
{
    /// <inheritdoc />
    public partial class RenameCodeToIptvUserAndAddFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserCode",
                table: "Customers",
                newName: "IptvUser");

            migrationBuilder.AddColumn<string>(
                name: "IptvPassword",
                table: "Customers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Customers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IptvPassword",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Customers");

            migrationBuilder.RenameColumn(
                name: "IptvUser",
                table: "Customers",
                newName: "UserCode");
        }
    }
}
