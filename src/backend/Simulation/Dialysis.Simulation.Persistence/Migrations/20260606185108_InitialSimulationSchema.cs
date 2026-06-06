using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Simulation.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSimulationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "transponder");

            migrationBuilder.EnsureSchema(
                name: "simulation");

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "transponder",
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
                name: "OutboxMessages",
                schema: "transponder",
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
                name: "SagaInstances",
                schema: "transponder",
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
                name: "SessionRecordLinks",
                schema: "simulation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SimulationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleSlug = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecordType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RealRecordId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRecordLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SimulationAuditEntries",
                schema: "simulation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SimulationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorContext = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Detail = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SimulationEvents",
                schema: "simulation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AggregateType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SimulationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientJourneyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TraceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SimulationSessions",
                schema: "simulation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScenarioId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OrganizationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TraceId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Seed = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    WorkflowState = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_SimulationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatientJourneys",
                schema: "simulation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SimulationSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicalRecordNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FamilyName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GivenName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    SexAtBirthCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    RealPatientId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientJourneys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientJourneys_SimulationSessions_SimulationSessionId",
                        column: x => x.SimulationSessionId,
                        principalSchema: "simulation",
                        principalTable: "SimulationSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_CompletedAtUtc",
                schema: "transponder",
                table: "InboxMessages",
                column: "CompletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_DeduplicationKey",
                schema: "transponder",
                table: "InboxMessages",
                column: "DeduplicationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc",
                schema: "transponder",
                table: "OutboxMessages",
                column: "ProcessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "UX_PatientJourneys_SessionId",
                schema: "simulation",
                table: "PatientJourneys",
                column: "SimulationSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SagaInstances_SagaKind_InstanceKey",
                schema: "transponder",
                table: "SagaInstances",
                columns: new[] { "SagaKind", "InstanceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionRecordLinks_SessionId",
                schema: "simulation",
                table: "SessionRecordLinks",
                column: "SimulationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationAuditEntries_SessionId",
                schema: "simulation",
                table: "SimulationAuditEntries",
                column: "SimulationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationEvents_SessionId",
                schema: "simulation",
                table: "SimulationEvents",
                column: "SimulationSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationSessions_TenantId",
                schema: "simulation",
                table: "SimulationSessions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "transponder");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "transponder");

            migrationBuilder.DropTable(
                name: "PatientJourneys",
                schema: "simulation");

            migrationBuilder.DropTable(
                name: "SagaInstances",
                schema: "transponder");

            migrationBuilder.DropTable(
                name: "SessionRecordLinks",
                schema: "simulation");

            migrationBuilder.DropTable(
                name: "SimulationAuditEntries",
                schema: "simulation");

            migrationBuilder.DropTable(
                name: "SimulationEvents",
                schema: "simulation");

            migrationBuilder.DropTable(
                name: "SimulationSessions",
                schema: "simulation");
        }
    }
}
