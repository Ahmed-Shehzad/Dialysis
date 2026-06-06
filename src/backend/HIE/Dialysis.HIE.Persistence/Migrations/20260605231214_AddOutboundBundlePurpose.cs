using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.HIE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboundBundlePurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                schema: "hie_outbound",
                table: "OutboundBundles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Purpose",
                schema: "hie_outbound",
                table: "OutboundBundles");
        }
    }
}
