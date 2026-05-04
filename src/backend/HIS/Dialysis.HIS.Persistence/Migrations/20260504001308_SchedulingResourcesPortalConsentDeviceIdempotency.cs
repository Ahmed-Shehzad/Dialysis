using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.HIS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SchedulingResourcesPortalConsentDeviceIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalMessageId",
                schema: "his_integration",
                table: "DeviceReadings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PortalConsentPreferences",
                schema: "his_portal",
                columns: table => new
                {
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SummaryVisible = table.Column<bool>(type: "bit", nullable: false),
                    AppointmentRequestsAllowed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortalConsentPreferences", x => x.PatientId);
                    table.ForeignKey(
                        name: "FK_PortalConsentPreferences_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "his_patientflow",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SchedulingResources",
                schema: "his_scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KindCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsBookable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulingResources", x => x.Id);
                });

            // Default demo resources (same IDs as HisSeed / HisDataSeeder); then any orphan Appointment.ResourceId.
            migrationBuilder.Sql(
                """
                INSERT INTO [his_scheduling].[SchedulingResources] ([Id], [KindCode], [DisplayName], [IsBookable])
                VALUES
                    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb001', N'room', N'Dialysis bay 1', 1),
                    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb002', N'equipment', N'Patient scale A', 1),
                    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb003', N'staff', N'Nurse slot — morning', 1);

                INSERT INTO [his_scheduling].[SchedulingResources] ([Id], [KindCode], [DisplayName], [IsBookable])
                SELECT DISTINCT a.[ResourceId], N'legacy', N'Imported resource', 1
                FROM [his_scheduling].[Appointments] AS a
                WHERE NOT EXISTS (
                    SELECT 1 FROM [his_scheduling].[SchedulingResources] AS r WHERE r.[Id] = a.[ResourceId]);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceReadings_ExternalMessageId",
                schema: "his_integration",
                table: "DeviceReadings",
                column: "ExternalMessageId",
                unique: true,
                filter: "[ExternalMessageId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ResourceId",
                schema: "his_scheduling",
                table: "Appointments",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_SchedulingResources_KindCode",
                schema: "his_scheduling",
                table: "SchedulingResources",
                column: "KindCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_SchedulingResources_ResourceId",
                schema: "his_scheduling",
                table: "Appointments",
                column: "ResourceId",
                principalSchema: "his_scheduling",
                principalTable: "SchedulingResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_SchedulingResources_ResourceId",
                schema: "his_scheduling",
                table: "Appointments");

            migrationBuilder.DropTable(
                name: "PortalConsentPreferences",
                schema: "his_portal");

            migrationBuilder.DropTable(
                name: "SchedulingResources",
                schema: "his_scheduling");

            migrationBuilder.DropIndex(
                name: "IX_DeviceReadings_ExternalMessageId",
                schema: "his_integration",
                table: "DeviceReadings");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ResourceId",
                schema: "his_scheduling",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ExternalMessageId",
                schema: "his_integration",
                table: "DeviceReadings");
        }
    }
}
