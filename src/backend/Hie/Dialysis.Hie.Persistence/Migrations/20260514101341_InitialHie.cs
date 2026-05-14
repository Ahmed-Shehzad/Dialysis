using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Hie.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialHie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hie_openehr");

            migrationBuilder.EnsureSchema(
                name: "hie_consent");

            migrationBuilder.EnsureSchema(
                name: "transponder");

            migrationBuilder.EnsureSchema(
                name: "hie_outbound");

            migrationBuilder.EnsureSchema(
                name: "hie_inbound");

            migrationBuilder.CreateTable(
                name: "Compositions",
                schema: "hie_openehr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArchetypeId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Composer = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CommittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compositions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Consents",
                schema: "hie_consent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Scope = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consents", x => x.Id);
                });

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
                name: "OutboundBundles",
                schema: "hie_outbound",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LogicalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PartnerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FhirJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFailureReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundBundles", x => x.Id);
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
                name: "PatientIndex",
                schema: "hie_inbound",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalLogicalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MedicalRecordNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FamilyName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    GivenName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    SexAtBirthCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientIndex", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceivedResources",
                schema: "hie_inbound",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LogicalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FhirJson = table.Column<string>(type: "text", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidationOutcome = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivedResources", x => x.Id);
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

            migrationBuilder.CreateIndex(
                name: "UX_Compositions_PatientArchetypeVersion",
                schema: "hie_openehr",
                table: "Compositions",
                columns: new[] { "PatientId", "ArchetypeId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consents_PatientPartnerScopeDirection",
                schema: "hie_consent",
                table: "Consents",
                columns: new[] { "PatientId", "PartnerId", "Scope", "Direction" });

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
                name: "IX_OutboundBundles_PatientId",
                schema: "hie_outbound",
                table: "OutboundBundles",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboundBundles_Status_NextAttempt",
                schema: "hie_outbound",
                table: "OutboundBundles",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc",
                schema: "transponder",
                table: "OutboxMessages",
                column: "ProcessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PatientIndex_Mrn",
                schema: "hie_inbound",
                table: "PatientIndex",
                column: "MedicalRecordNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PatientIndex_Name",
                schema: "hie_inbound",
                table: "PatientIndex",
                columns: new[] { "FamilyName", "GivenName" });

            migrationBuilder.CreateIndex(
                name: "UX_PatientIndex_PartnerExternalId",
                schema: "hie_inbound",
                table: "PatientIndex",
                columns: new[] { "PartnerId", "ExternalLogicalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ReceivedResources_PartnerLogicalId",
                schema: "hie_inbound",
                table: "ReceivedResources",
                columns: new[] { "PartnerId", "ResourceType", "LogicalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SagaInstances_SagaKind_InstanceKey",
                schema: "transponder",
                table: "SagaInstances",
                columns: new[] { "SagaKind", "InstanceKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Compositions",
                schema: "hie_openehr");

            migrationBuilder.DropTable(
                name: "Consents",
                schema: "hie_consent");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "transponder");

            migrationBuilder.DropTable(
                name: "OutboundBundles",
                schema: "hie_outbound");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "transponder");

            migrationBuilder.DropTable(
                name: "PatientIndex",
                schema: "hie_inbound");

            migrationBuilder.DropTable(
                name: "ReceivedResources",
                schema: "hie_inbound");

            migrationBuilder.DropTable(
                name: "SagaInstances",
                schema: "transponder");
        }
    }
}
