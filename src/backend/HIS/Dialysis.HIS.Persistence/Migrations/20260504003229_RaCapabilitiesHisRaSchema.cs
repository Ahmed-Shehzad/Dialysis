using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.HIS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RaCapabilitiesHisRaSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "his_ra");

            migrationBuilder.CreateTable(
                name: "RaAnalyticsExportJobs",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PipelineCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChecksAppliedJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    SafeToProceed = table.Column<bool>(type: "bit", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentTypeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExternalSystemCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExternalUri = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ExchangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastHandshakeAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorpusCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SearchText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IndexedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicationOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BarcodeToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DispensedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ThreadCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RaisedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClearedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaQualityWorkflowTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaSecurityMechanismHardenings",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MechanismCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AppliedLevel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AssessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaSecurityMechanismHardenings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaWaitlistEntries",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceKindCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequestedNotBeforeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnqueuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaWaitlistEntries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "RaSecurityMechanismHardenings",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaWaitlistEntries",
                schema: "his_ra");
        }
    }
}
