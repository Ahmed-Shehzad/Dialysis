using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelMetadataFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentsJson",
                schema: "smartconnect",
                table: "IntegrationFlows",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataTypesJson",
                schema: "smartconnect",
                table: "IntegrationFlows",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DependenciesJson",
                schema: "smartconnect",
                table: "IntegrationFlows",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentsJson",
                schema: "smartconnect",
                table: "IntegrationFlows");

            migrationBuilder.DropColumn(
                name: "DataTypesJson",
                schema: "smartconnect",
                table: "IntegrationFlows");

            migrationBuilder.DropColumn(
                name: "DependenciesJson",
                schema: "smartconnect",
                table: "IntegrationFlows");
        }
    }
}
