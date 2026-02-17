using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseDomainEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawJson",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "RawJson",
                table: "observations");

            migrationBuilder.AlterColumn<decimal>(
                name: "NumericValue",
                table: "observations",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RawJson",
                table: "patients",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "NumericValue",
                table: "observations",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawJson",
                table: "observations",
                type: "text",
                nullable: true);
        }
    }
}
