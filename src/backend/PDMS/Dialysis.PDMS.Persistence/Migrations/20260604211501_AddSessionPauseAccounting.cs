#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.PDMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionPauseAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "AccumulatedPausedDuration",
                schema: "pdms_sessions",
                table: "DialysisSessions",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<DateTime>(
                name: "PausedAtUtc",
                schema: "pdms_sessions",
                table: "DialysisSessions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccumulatedPausedDuration",
                schema: "pdms_sessions",
                table: "DialysisSessions");

            migrationBuilder.DropColumn(
                name: "PausedAtUtc",
                schema: "pdms_sessions",
                table: "DialysisSessions");
        }
    }
}
