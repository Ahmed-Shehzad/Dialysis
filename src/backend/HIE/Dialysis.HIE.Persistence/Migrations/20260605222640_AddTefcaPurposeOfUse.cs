using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.HIE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTefcaPurposeOfUse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedPurposes",
                schema: "hie_tefca",
                table: "QhinPartners",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                schema: "hie_consent",
                table: "Consents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedPurposes",
                schema: "hie_tefca",
                table: "QhinPartners");

            migrationBuilder.DropColumn(
                name: "Purpose",
                schema: "hie_consent",
                table: "Consents");
        }
    }
}
