#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.PDMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PdmsFhirAuditExportSubscriptionsScaffold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fhir_audit");

            migrationBuilder.EnsureSchema(
                name: "fhir_export");

            migrationBuilder.EnsureSchema(
                name: "fhir_subscriptions");

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "fhir_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModuleSlug = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Subtype = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AgentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ResourceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ResourceJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "export_jobs",
                schema: "fhir_export",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    GroupId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ResourceTypesCsv = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Since = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeIdentificationProfile = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RequestorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    OutputsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_export_jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notification_outbox",
                schema: "fhir_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    EnqueuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_outbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "fhir_subscriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TopicUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ChannelType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChannelEndpoint = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ChannelHeader = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    FilterParametersJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastNotificationAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_RecordedAt",
                schema: "fhir_audit",
                table: "audit_events",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_ResourceType_ResourceId",
                schema: "fhir_audit",
                table: "audit_events",
                columns: new[] { "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_export_jobs_Status",
                schema: "fhir_export",
                table: "export_jobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_SubscriptionId_DeliveredAt",
                schema: "fhir_subscriptions",
                table: "notification_outbox",
                columns: new[] { "SubscriptionId", "DeliveredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_TopicUrl_Status",
                schema: "fhir_subscriptions",
                table: "subscriptions",
                columns: new[] { "TopicUrl", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "fhir_audit");

            migrationBuilder.DropTable(
                name: "export_jobs",
                schema: "fhir_export");

            migrationBuilder.DropTable(
                name: "notification_outbox",
                schema: "fhir_subscriptions");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "fhir_subscriptions");
        }
    }
}
