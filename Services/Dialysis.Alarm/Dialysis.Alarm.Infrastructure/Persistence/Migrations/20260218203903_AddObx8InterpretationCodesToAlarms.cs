using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Alarm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddObx8InterpretationCodesToAlarms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Abnormality",
                table: "Alarms",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterpretationType",
                table: "Alarms",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Alarms",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Abnormality",
                table: "Alarms");

            migrationBuilder.DropColumn(
                name: "InterpretationType",
                table: "Alarms");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Alarms");
        }
    }
}
