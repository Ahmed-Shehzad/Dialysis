using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionEncounterId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncounterId",
                table: "sessions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncounterId",
                table: "sessions");
        }
    }
}
