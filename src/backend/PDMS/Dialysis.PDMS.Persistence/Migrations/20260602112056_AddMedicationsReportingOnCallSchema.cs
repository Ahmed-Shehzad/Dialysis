#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Dialysis.PDMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationsReportingOnCallSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pdms_oncall");

            migrationBuilder.EnsureSchema(
                name: "pdms_medications");

            migrationBuilder.EnsureSchema(
                name: "pdms_reporting");

            migrationBuilder.CreateTable(
                name: "AlarmDispatches",
                schema: "pdms_oncall",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InfusionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChairId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlarmCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RotationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentLinkIndex = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AcknowledgedBySub = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_AlarmDispatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EscalationPolicies",
                schema: "pdms_oncall",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CriticalPrimaryWindow = table.Column<TimeSpan>(type: "interval", nullable: false),
                    CriticalBackupWindow = table.Column<TimeSpan>(type: "interval", nullable: false),
                    WarningPrimaryWindow = table.Column<TimeSpan>(type: "interval", nullable: false),
                    WarningBackupWindow = table.Column<TimeSpan>(type: "interval", nullable: false),
                    InformationalPrimaryWindow = table.Column<TimeSpan>(type: "interval", nullable: false),
                    QuietHoursSuppressNonCritical = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_EscalationPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IvPumpInfusions",
                schema: "pdms_medications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChairId = table.Column<Guid>(type: "uuid", nullable: false),
                    PumpDeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    VendorCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MedicationCodeSystem = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MedicationCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MedicationDisplay = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProgrammedRateMlPerHour = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    ActualRateMlPerHour = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    ProgrammedVolumeMl = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    InfusedVolumeMl = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_IvPumpInfusions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicationAdministrationRecords",
                schema: "pdms_medications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_MedicationAdministrationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicationInventoryItems",
                schema: "pdms_medications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicationCodeSystem = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MedicationCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MedicationDisplay = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiryUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OnHandUnits = table.Column<int>(type: "integer", nullable: false),
                    Threshold = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_MedicationInventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnCallRotations",
                schema: "pdms_oncall",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChairId = table.Column<Guid>(type: "uuid", nullable: false),
                    Shift = table.Column<string>(type: "jsonb", nullable: false),
                    EffectiveFromUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveUntilUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    Primary = table.Column<string>(type: "jsonb", nullable: false),
                    Backup = table.Column<string>(type: "jsonb", nullable: false),
                    Supervisor = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_OnCallRotations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportTemplates",
                schema: "pdms_reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PublishedVersionNumber = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("PK_ReportTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionReports",
                schema: "pdms_reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Format = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    StorageRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_SessionReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlarmDispatchAttempts",
                schema: "pdms_oncall",
                columns: table => new
                {
                    AlarmDispatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChainLinkIndex = table.Column<int>(type: "integer", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Delivered = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttemptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmDispatchAttempts", x => new { x.AlarmDispatchId, x.Sequence });
                    table.ForeignKey(
                        name: "FK_AlarmDispatchAttempts_AlarmDispatches_AlarmDispatchId",
                        column: x => x.AlarmDispatchId,
                        principalSchema: "pdms_oncall",
                        principalTable: "AlarmDispatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicationAdministrationEntries",
                schema: "pdms_medications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicationCodeSystem = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MedicationCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MedicationDisplay = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DoseQuantity = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    DoseUnit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Route = table.Column<int>(type: "integer", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorSub = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    WasAdministered = table.Column<bool>(type: "boolean", nullable: false),
                    DeclineReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RelatedOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    MarId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationAdministrationEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationAdministrationEntries_MedicationAdministrationRec~",
                        column: x => x.MarId,
                        principalSchema: "pdms_medications",
                        principalTable: "MedicationAdministrationRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportTemplateVersions",
                schema: "pdms_reporting",
                columns: table => new
                {
                    VersionNumber = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    BodyMarkdown = table.Column<string>(type: "text", nullable: false),
                    AuthoredBySub = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AuthoredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTemplateVersions", x => new { x.TemplateId, x.VersionNumber });
                    table.ForeignKey(
                        name: "FK_ReportTemplateVersions_ReportTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "pdms_reporting",
                        principalTable: "ReportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmDispatches_ChairId_StartedAtUtc",
                schema: "pdms_oncall",
                table: "AlarmDispatches",
                columns: new[] { "ChairId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmDispatches_SessionId",
                schema: "pdms_oncall",
                table: "AlarmDispatches",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AlarmDispatches_Status",
                schema: "pdms_oncall",
                table: "AlarmDispatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationPolicies_Name",
                schema: "pdms_oncall",
                table: "EscalationPolicies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IvPumpInfusions_ChairId",
                schema: "pdms_medications",
                table: "IvPumpInfusions",
                column: "ChairId");

            migrationBuilder.CreateIndex(
                name: "IX_IvPumpInfusions_SessionId_PumpDeviceId_Status",
                schema: "pdms_medications",
                table: "IvPumpInfusions",
                columns: new[] { "SessionId", "PumpDeviceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdministrationEntries_MarId",
                schema: "pdms_medications",
                table: "MedicationAdministrationEntries",
                column: "MarId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdministrationEntries_OccurredAtUtc",
                schema: "pdms_medications",
                table: "MedicationAdministrationEntries",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdministrationRecords_PatientId",
                schema: "pdms_medications",
                table: "MedicationAdministrationRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdministrationRecords_SessionId",
                schema: "pdms_medications",
                table: "MedicationAdministrationRecords",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationInventoryItems_MedicationCodeSystem_MedicationCode",
                schema: "pdms_medications",
                table: "MedicationInventoryItems",
                columns: new[] { "MedicationCodeSystem", "MedicationCode" });

            migrationBuilder.CreateIndex(
                name: "IX_OnCallRotations_ChairId_EffectiveFromUtc_EffectiveUntilUtc",
                schema: "pdms_oncall",
                table: "OnCallRotations",
                columns: new[] { "ChairId", "EffectiveFromUtc", "EffectiveUntilUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportTemplates_Slug_Kind",
                schema: "pdms_reporting",
                table: "ReportTemplates",
                columns: new[] { "Slug", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionReports_PatientId_Kind",
                schema: "pdms_reporting",
                table: "SessionReports",
                columns: new[] { "PatientId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionReports_SessionId",
                schema: "pdms_reporting",
                table: "SessionReports",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmDispatchAttempts",
                schema: "pdms_oncall");

            migrationBuilder.DropTable(
                name: "EscalationPolicies",
                schema: "pdms_oncall");

            migrationBuilder.DropTable(
                name: "IvPumpInfusions",
                schema: "pdms_medications");

            migrationBuilder.DropTable(
                name: "MedicationAdministrationEntries",
                schema: "pdms_medications");

            migrationBuilder.DropTable(
                name: "MedicationInventoryItems",
                schema: "pdms_medications");

            migrationBuilder.DropTable(
                name: "OnCallRotations",
                schema: "pdms_oncall");

            migrationBuilder.DropTable(
                name: "ReportTemplateVersions",
                schema: "pdms_reporting");

            migrationBuilder.DropTable(
                name: "SessionReports",
                schema: "pdms_reporting");

            migrationBuilder.DropTable(
                name: "AlarmDispatches",
                schema: "pdms_oncall");

            migrationBuilder.DropTable(
                name: "MedicationAdministrationRecords",
                schema: "pdms_medications");

            migrationBuilder.DropTable(
                name: "ReportTemplates",
                schema: "pdms_reporting");
        }
    }
}
