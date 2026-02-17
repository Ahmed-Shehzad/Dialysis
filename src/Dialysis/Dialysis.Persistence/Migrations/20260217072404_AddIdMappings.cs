using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "id_mappings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LocalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExternalSystem = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_id_mappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_id_mappings_TenantId_ExternalSystem_ExternalId",
                table: "id_mappings",
                columns: new[] { "TenantId", "ExternalSystem", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_id_mappings_TenantId_ResourceType_LocalId_ExternalSystem",
                table: "id_mappings",
                columns: new[] { "TenantId", "ResourceType", "LocalId", "ExternalSystem" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "id_mappings");
        }
    }
}
