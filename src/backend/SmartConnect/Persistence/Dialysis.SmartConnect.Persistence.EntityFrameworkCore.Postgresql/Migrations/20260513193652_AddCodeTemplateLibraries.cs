#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeTemplateLibraries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "smartconnect",
                table: "IntegrationFlows",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                schema: "smartconnect",
                table: "IntegrationFlows",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                schema: "smartconnect",
                table: "IntegrationFlows",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                schema: "smartconnect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AttributesJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeTemplateLibraries",
                schema: "smartconnect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LinkedFlowIdsJson = table.Column<string>(type: "text", nullable: false),
                    AutoLinkNewFlows = table.Column<bool>(type: "boolean", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeTemplateLibraries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlowGroups",
                schema: "smartconnect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VariableMapEntries",
                schema: "smartconnect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariableMapEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CodeTemplates",
                schema: "smartconnect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LibraryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ContextsJson = table.Column<string>(type: "text", nullable: false),
                    JsDoc = table.Column<string>(type: "text", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeTemplates_CodeTemplateLibraries_LibraryId",
                        column: x => x.LibraryId,
                        principalSchema: "smartconnect",
                        principalTable: "CodeTemplateLibraries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationFlows_GroupId",
                schema: "smartconnect",
                table: "IntegrationFlows",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Category_Timestamp",
                schema: "smartconnect",
                table: "AuditEvents",
                columns: new[] { "Category", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_FlowId",
                schema: "smartconnect",
                table: "AuditEvents",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeTemplates_LibraryId",
                schema: "smartconnect",
                table: "CodeTemplates",
                column: "LibraryId");

            migrationBuilder.CreateIndex(
                name: "IX_VariableMapEntries_Scope_FlowId_Key",
                schema: "smartconnect",
                table: "VariableMapEntries",
                columns: new[] { "Scope", "FlowId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvents",
                schema: "smartconnect");

            migrationBuilder.DropTable(
                name: "CodeTemplates",
                schema: "smartconnect");

            migrationBuilder.DropTable(
                name: "FlowGroups",
                schema: "smartconnect");

            migrationBuilder.DropTable(
                name: "VariableMapEntries",
                schema: "smartconnect");

            migrationBuilder.DropTable(
                name: "CodeTemplateLibraries",
                schema: "smartconnect");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationFlows_GroupId",
                schema: "smartconnect",
                table: "IntegrationFlows");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "smartconnect",
                table: "IntegrationFlows");

            migrationBuilder.DropColumn(
                name: "GroupId",
                schema: "smartconnect",
                table: "IntegrationFlows");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                schema: "smartconnect",
                table: "IntegrationFlows");
        }
    }
}
