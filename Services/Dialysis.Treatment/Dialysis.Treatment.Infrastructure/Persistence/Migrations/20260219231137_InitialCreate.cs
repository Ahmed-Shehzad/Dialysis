using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Treatment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TreatmentSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "default"),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    PatientMrn = table.Column<string>(type: "text", nullable: true),
                    DeviceId = table.Column<string>(type: "text", nullable: true),
                    DeviceEui64 = table.Column<string>(type: "text", nullable: true),
                    TherapyId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Mode = table.Column<string>(type: "text", nullable: true),
                    Modality = table.Column<string>(type: "text", nullable: true),
                    Phase = table.Column<string>(type: "text", nullable: true),
                    TherapyTimePrescribedMin = table.Column<int>(type: "integer", nullable: true),
                    ObservationCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Observations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TreatmentSessionId = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    SubId = table.Column<string>(type: "text", nullable: true),
                    ReferenceRange = table.Column<string>(type: "text", nullable: true),
                    ResultStatus = table.Column<string>(type: "text", nullable: true),
                    EffectiveTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Provenance = table.Column<string>(type: "text", nullable: true),
                    EquipmentInstanceId = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<string>(type: "text", nullable: true),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MessageTimeDriftSeconds = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Observations_TreatmentSessions_TreatmentSessionId",
                        column: x => x.TreatmentSessionId,
                        principalTable: "TreatmentSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Observations_SessionId_ObservedAtUtc",
                table: "Observations",
                columns: new[] { "TreatmentSessionId", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Observations_TreatmentSessionId",
                table: "Observations",
                column: "TreatmentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_TreatmentSessionId_Code_SubId",
                table: "Observations",
                columns: new[] { "TreatmentSessionId", "Code", "SubId" });

            migrationBuilder.CreateIndex(
                name: "IX_Observations_TreatmentSessionId_Level",
                table: "Observations",
                columns: new[] { "TreatmentSessionId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSessions_DeviceId",
                table: "TreatmentSessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSessions_PatientMrn",
                table: "TreatmentSessions",
                column: "PatientMrn");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSessions_Status",
                table: "TreatmentSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSessions_TenantId_SessionId",
                table: "TreatmentSessions",
                columns: new[] { "TenantId", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSessions_TenantId_StartedAt",
                table: "TreatmentSessions",
                columns: new[] { "TenantId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Observations");

            migrationBuilder.DropTable(
                name: "TreatmentSessions");
        }
    }
}
