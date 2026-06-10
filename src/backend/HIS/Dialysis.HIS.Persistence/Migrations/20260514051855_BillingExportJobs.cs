#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.HIS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BillingExportJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "his_operations");

            migrationBuilder.EnsureSchema(
                name: "his_data");

            migrationBuilder.EnsureSchema(
                name: "his_integration");

            migrationBuilder.EnsureSchema(
                name: "transponder");

            migrationBuilder.EnsureSchema(
                name: "his_ra");

            migrationBuilder.CreateTable(
                name: "BillingExportJobs",
                schema: "his_operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayerCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StatusCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingExportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataImportJobs",
                schema: "his_data",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDescription = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatusCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ValidationSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceReadings",
                schema: "his_integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExternalMessageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceReadings", x => x.Id);
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
                name: "InventoryItems",
                schema: "his_operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    QuantityOnHand = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
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
                name: "RaAnalyticsExportJobs",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatusCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaAnalyticsExportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaClinicalDecisionSupportEvaluations",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChecksAppliedJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    SafeToProceed = table.Column<bool>(type: "boolean", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaClinicalDecisionSupportEvaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaEhrDocumentExchangeRecords",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTypeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalSystemCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalUri = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ExchangedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaEhrDocumentExchangeRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaFinancialErpLinks",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LastHandshakeAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StatusCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaFinancialErpLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaFullTextSearchEntries",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorpusCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SearchText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IndexedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaFullTextSearchEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaMedicationDispensingRecords",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicationOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BarcodeToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DispensedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaMedicationDispensingRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaOrgCommunications",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaOrgCommunications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaPatientAlerts",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RaisedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClearedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaPatientAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaQualityWorkflowTasks",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StatusCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaQualityWorkflowTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaResearchEducationActivities",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityKindCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaResearchEducationActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaSecurityMechanismHardenings",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MechanismCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AppliedLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AssessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaSecurityMechanismHardenings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaSpecialistEncounterRecords",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialtyCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalSystemCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaSpecialistEncounterRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaWaitlistEntries",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceKindCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RequestedNotBeforeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EnqueuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaWaitlistEntries", x => x.Id);
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
                name: "StaffMembers",
                schema: "his_operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PrimaryRoleCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMembers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingExportJobs_StatusCode",
                schema: "his_operations",
                table: "BillingExportJobs",
                column: "StatusCode");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceReadings_ExternalMessageId",
                schema: "his_integration",
                table: "DeviceReadings",
                column: "ExternalMessageId",
                unique: true,
                filter: "\"ExternalMessageId\" IS NOT NULL");

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
                name: "BillingExportJobs",
                schema: "his_operations");

            migrationBuilder.DropTable(
                name: "DataImportJobs",
                schema: "his_data");

            migrationBuilder.DropTable(
                name: "DeviceReadings",
                schema: "his_integration");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "transponder");

            migrationBuilder.DropTable(
                name: "InventoryItems",
                schema: "his_operations");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "transponder");

            migrationBuilder.DropTable(
                name: "RaAnalyticsExportJobs",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaClinicalDecisionSupportEvaluations",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaEhrDocumentExchangeRecords",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaFinancialErpLinks",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaFullTextSearchEntries",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaMedicationDispensingRecords",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaOrgCommunications",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaPatientAlerts",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaQualityWorkflowTasks",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaResearchEducationActivities",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaSecurityMechanismHardenings",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaSpecialistEncounterRecords",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaWaitlistEntries",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "SagaInstances",
                schema: "transponder");

            migrationBuilder.DropTable(
                name: "StaffMembers",
                schema: "his_operations");
        }
    }
}
