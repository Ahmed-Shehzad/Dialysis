using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Alarm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageTimeDriftSecondsToAlarms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "MessageTimeDriftSeconds",
                table: "Alarms",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageTimeDriftSeconds",
                table: "Alarms");
        }
    }
}
