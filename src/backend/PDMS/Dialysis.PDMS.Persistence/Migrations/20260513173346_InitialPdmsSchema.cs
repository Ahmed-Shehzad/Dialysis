#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.PDMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPdmsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pdms_sessions");

            migrationBuilder.EnsureSchema(
                name: "pdms");

            migrationBuilder.EnsureSchema(
                name: "pdms_telemetry");

            migrationBuilder.CreateTable(
                name: "DialysisSessions",
                schema: "pdms_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DialyzerModel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PrescribedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    BloodFlowRateMlPerMin = table.Column<int>(type: "integer", nullable: false),
                    DialysateFlowRateMlPerMin = table.Column<int>(type: "integer", nullable: false),
                    DialysatePotassiumMmolPerL = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DialysateCalciumMmolPerL = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DialysateSodiumMmolPerL = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TargetUfVolumeLiters = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    AnticoagulationProtocolCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AccessKind = table.Column<int>(type: "integer", nullable: false),
                    AccessSite = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AccessEstablishedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    AchievedUfVolumeLiters = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: true),
                    AbortReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DialysisSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "pdms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeduplicationKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RoutingKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Machines",
                schema: "pdms_telemetry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VendorCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ModelCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LastSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Machines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObservationCodes",
                schema: "pdms_telemetry",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Units = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsVendorSpecific = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObservationCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "pdms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssemblyQualifiedEventType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    W3CTraceParent = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RawHl7Messages",
                schema: "pdms_telemetry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    MessageControlId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Payload = table.Column<byte[]>(type: "bytea", nullable: false),
                    ProcessingStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawHl7Messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SagaInstances",
                schema: "pdms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SagaKind = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    InstanceKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    StateName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StateJson = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentAlarms",
                schema: "pdms_telemetry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlarmCode = table.Column<long>(type: "bigint", nullable: false),
                    AlarmSource = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AlarmPhase = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    State = table.Column<int>(type: "integer", nullable: false),
                    FirstObservedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastObservedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentAlarms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentObservations",
                schema: "pdms_telemetry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MachineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MdcCode = table.Column<long>(type: "bigint", nullable: false),
                    ContainmentPath = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ValueNumeric = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    ValueString = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Units = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ProfileValues = table.Column<decimal[]>(type: "numeric[]", nullable: true),
                    ProfileTimesSeconds = table.Column<int[]>(type: "integer[]", nullable: true),
                    SourceMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentObservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntradialyticReadings",
                schema: "pdms_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SystolicBloodPressure = table.Column<int>(type: "integer", nullable: false),
                    DiastolicBloodPressure = table.Column<int>(type: "integer", nullable: false),
                    HeartRateBpm = table.Column<int>(type: "integer", nullable: false),
                    ArterialPressureMmHg = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    VenousPressureMmHg = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    UltrafiltrationRateMlPerHour = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    ConductivityMsPerCm = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntradialyticReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntradialyticReadings_DialysisSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "pdms_sessions",
                        principalTable: "DialysisSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DialysisSessions_MachineId",
                schema: "pdms_sessions",
                table: "DialysisSessions",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_DialysisSessions_PatientId",
                schema: "pdms_sessions",
                table: "DialysisSessions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DialysisSessions_ScheduledStartUtc",
                schema: "pdms_sessions",
                table: "DialysisSessions",
                column: "ScheduledStartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_CompletedAtUtc",
                schema: "pdms",
                table: "InboxMessages",
                column: "CompletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_DeduplicationKey",
                schema: "pdms",
                table: "InboxMessages",
                column: "DeduplicationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntradialyticReadings_SessionId_ObservedAtUtc",
                schema: "pdms_sessions",
                table: "IntradialyticReadings",
                columns: new[] { "SessionId", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Machines_SerialNumber",
                schema: "pdms_telemetry",
                table: "Machines",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc",
                schema: "pdms",
                table: "OutboxMessages",
                column: "ProcessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RawHl7Messages_MachineId_ReceivedAtUtc",
                schema: "pdms_telemetry",
                table: "RawHl7Messages",
                columns: new[] { "MachineId", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RawHl7Messages_MessageControlId",
                schema: "pdms_telemetry",
                table: "RawHl7Messages",
                column: "MessageControlId");

            migrationBuilder.CreateIndex(
                name: "IX_SagaInstances_SagaKind_InstanceKey",
                schema: "pdms",
                table: "SagaInstances",
                columns: new[] { "SagaKind", "InstanceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentAlarms_MachineId_State",
                schema: "pdms_telemetry",
                table: "TreatmentAlarms",
                columns: new[] { "MachineId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentAlarms_SessionId_FirstObservedUtc",
                schema: "pdms_telemetry",
                table: "TreatmentAlarms",
                columns: new[] { "SessionId", "FirstObservedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentObservations_MachineId_ObservedAtUtc",
                schema: "pdms_telemetry",
                table: "TreatmentObservations",
                columns: new[] { "MachineId", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentObservations_MdcCode",
                schema: "pdms_telemetry",
                table: "TreatmentObservations",
                column: "MdcCode");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentObservations_SessionId_ObservedAtUtc",
                schema: "pdms_telemetry",
                table: "TreatmentObservations",
                columns: new[] { "SessionId", "ObservedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "pdms");

            migrationBuilder.DropTable(
                name: "IntradialyticReadings",
                schema: "pdms_sessions");

            migrationBuilder.DropTable(
                name: "Machines",
                schema: "pdms_telemetry");

            migrationBuilder.DropTable(
                name: "ObservationCodes",
                schema: "pdms_telemetry");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "pdms");

            migrationBuilder.DropTable(
                name: "RawHl7Messages",
                schema: "pdms_telemetry");

            migrationBuilder.DropTable(
                name: "SagaInstances",
                schema: "pdms");

            migrationBuilder.DropTable(
                name: "TreatmentAlarms",
                schema: "pdms_telemetry");

            migrationBuilder.DropTable(
                name: "TreatmentObservations",
                schema: "pdms_telemetry");

            migrationBuilder.DropTable(
                name: "DialysisSessions",
                schema: "pdms_sessions");
        }
    }
}
