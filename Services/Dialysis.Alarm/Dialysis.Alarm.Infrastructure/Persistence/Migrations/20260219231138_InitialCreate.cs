using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Alarm.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alarms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "default"),
                    AlarmType = table.Column<string>(type: "text", nullable: true),
                    SourceCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceLimits = table.Column<string>(type: "text", nullable: true),
                    Priority = table.Column<string>(type: "text", nullable: true),
                    InterpretationType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Abnormality = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    EventPhase = table.Column<string>(type: "text", nullable: false),
                    AlarmState = table.Column<string>(type: "text", nullable: false),
                    ActivityState = table.Column<string>(type: "text", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MessageTimeDriftSeconds = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarms", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_OccurredAt",
                table: "Alarms",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_TenantId_DeviceId",
                table: "Alarms",
                columns: new[] { "TenantId", "DeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_TenantId_SessionId",
                table: "Alarms",
                columns: new[] { "TenantId", "SessionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alarms");
        }
    }
}
