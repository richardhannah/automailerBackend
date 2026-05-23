using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AutoMailerBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowEmailSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowEmailSettings",
                columns: table => new
                {
                    WorkflowEmailSettingId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowType = table.Column<string>(type: "text", nullable: false),
                    RecipientType = table.Column<string>(type: "text", nullable: false),
                    EmailTemplateId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowEmailSettings", x => x.WorkflowEmailSettingId);
                    table.ForeignKey(
                        name: "FK_WorkflowEmailSettings_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "EmailTemplateId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEmailSettings_EmailTemplateId",
                table: "WorkflowEmailSettings",
                column: "EmailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEmailSettings_WorkflowType_RecipientType",
                table: "WorkflowEmailSettings",
                columns: new[] { "WorkflowType", "RecipientType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowEmailSettings");
        }
    }
}
