using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dialysis.EHR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EhrInitialWithFhirSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ehr_chart");

            migrationBuilder.EnsureSchema(
                name: "ehr_scheduling");

            migrationBuilder.EnsureSchema(
                name: "fhir_audit");

            migrationBuilder.EnsureSchema(
                name: "ehr_billing");

            migrationBuilder.EnsureSchema(
                name: "ehr_clinical");

            migrationBuilder.EnsureSchema(
                name: "fhir_export");

            migrationBuilder.EnsureSchema(
                name: "ehr");

            migrationBuilder.EnsureSchema(
                name: "ehr_integration");

            migrationBuilder.EnsureSchema(
                name: "fhir_subscriptions");

            migrationBuilder.EnsureSchema(
                name: "ehr_registration");

            migrationBuilder.EnsureSchema(
                name: "ehr_portal");

            migrationBuilder.CreateTable(
                name: "Allergies",
                schema: "ehr_chart",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeSystem = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CodeValue = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CodeDisplay = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ReactionText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    VerificationStatus = table.Column<int>(type: "integer", nullable: false),
                    OnsetDate = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_Allergies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                schema: "ehr_scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EncounterClassCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    VisitReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CheckedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                });

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
                name: "Charges",
                schema: "ehr_billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CptCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedClaimId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiagnosisPointerIcd10Codes = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_Charges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                schema: "ehr_billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayerCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ClaimFormatCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ExternalControlNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ChargeIds = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_Claims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalNotes",
                schema: "ehr_clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthoringProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subjective = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Objective = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Assessment = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Plan = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SignedByProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_ClinicalNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Encounters",
                schema: "ehr_clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    EncounterClassCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_Encounters", x => x.Id);
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
                name: "Immunizations",
                schema: "ehr_chart",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeSystem = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CodeValue = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CodeDisplay = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    AdministeredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SiteCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AdministeringProviderId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_Immunizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "ehr",
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
                name: "InsurerTransmissions",
                schema: "ehr_integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayerCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ClaimFormatCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalControlNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayloadDigest = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
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
                    table.PrimaryKey("PK_InsurerTransmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabOrders",
                schema: "ehr_clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderingProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabFacilityCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TransmissionFormat = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CancellationReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LoincPanelCodes = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_LabOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabResults",
                schema: "ehr_clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoincCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ValueText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ReferenceRangeText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AbnormalFlag = table.Column<int>(type: "integer", nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_LabResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabTransmissions",
                schema: "ehr_integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LabOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabFacilityCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TransmissionFormat = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayloadDigest = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExternalControlNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
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
                    table.PrimaryKey("PK_LabTransmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicationStatements",
                schema: "ehr_chart",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeSystem = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CodeValue = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CodeDisplay = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DoseText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FrequencyText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StartedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    StoppedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReasonText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_MedicationStatements", x => x.Id);
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
                name: "OutboxMessages",
                schema: "ehr",
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
                name: "Patients",
                schema: "ehr_registration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicalRecordNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FamilyName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GivenName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PrefixName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SuffixName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    SexAtBirthCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    PreferredLanguageCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    AddressLine1 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AddressLine2 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AddressCity = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AddressState = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AddressPostalCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    AddressCountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SupersededByPatientId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payers",
                schema: "ehr_billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayerCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ClearinghouseCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Payers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "ehr_billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    PostedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
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
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PharmacyTransmissions",
                schema: "ehr_integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PharmacyNcpdpId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TransmissionFormat = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayloadDigest = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExternalControlNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastErrorCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
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
                    table.PrimaryKey("PK_PharmacyTransmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortalAppointmentRequests",
                schema: "ehr_portal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReasonText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EarliestPreferredUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LatestPreferredUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    StaffNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_PortalAppointmentRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                schema: "ehr_clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrescribingProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicationRxnormCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MedicationDisplay = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DoseText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FrequencyText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    QuantityDispensed = table.Column<int>(type: "integer", nullable: false),
                    RefillsAuthorized = table.Column<int>(type: "integer", nullable: false),
                    PharmacyNcpdpId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TransmissionFormat = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CancellationReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProblemListItems",
                schema: "ehr_chart",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeSystem = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CodeValue = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CodeDisplay = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OnsetDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ResolvedDate = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_ProblemListItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderAvailabilityWindows",
                schema: "ehr_scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SlotDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_ProviderAvailabilityWindows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                schema: "ehr_registration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NationalProviderIdentifier = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    FamilyName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GivenName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    MiddleName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PrefixName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SuffixName = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    SpecialtyCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LicenseNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Remittances",
                schema: "ehr_billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayerCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AdjustmentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustmentCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AdjudicationStatus = table.Column<int>(type: "integer", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_Remittances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SagaInstances",
                schema: "ehr",
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
                name: "SecureMessages",
                schema: "ehr_portal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetProviderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_SecureMessages", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "VitalSignReadings",
                schema: "ehr_chart",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: true),
                    CodeSystem = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CodeValue = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CodeDisplay = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordedByProviderId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_VitalSignReadings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EncounterDiagnoses",
                schema: "ehr_clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Icd10Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Display = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_EncounterDiagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EncounterDiagnoses_Encounters_EncounterId",
                        column: x => x.EncounterId,
                        principalSchema: "ehr_clinical",
                        principalTable: "Encounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerformedProcedures",
                schema: "ehr_clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CptCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Display = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ModifierCodes = table.Column<string>(type: "text", nullable: false),
                    PerformedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PerformingProviderId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_PerformedProcedures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformedProcedures_Encounters_EncounterId",
                        column: x => x.EncounterId,
                        principalSchema: "ehr_clinical",
                        principalTable: "Encounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientContactPoints",
                schema: "ehr_registration",
                columns: table => new
                {
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    System = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Use = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientContactPoints", x => new { x.PatientId, x.Id });
                    table.ForeignKey(
                        name: "FK_PatientContactPoints_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "ehr_registration",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allergies_PatientId",
                schema: "ehr_chart",
                table: "Allergies",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                schema: "ehr_scheduling",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ProviderId_StartUtc",
                schema: "ehr_scheduling",
                table: "Appointments",
                columns: new[] { "ProviderId", "StartUtc" });

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
                name: "IX_Charges_EncounterId",
                schema: "ehr_billing",
                table: "Charges",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_Charges_PatientId",
                schema: "ehr_billing",
                table: "Charges",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ExternalControlNumber",
                schema: "ehr_billing",
                table: "Claims",
                column: "ExternalControlNumber",
                unique: true,
                filter: "\"ExternalControlNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Claims_PatientId",
                schema: "ehr_billing",
                table: "Claims",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_EncounterId",
                schema: "ehr_clinical",
                table: "ClinicalNotes",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_EncounterDiagnoses_EncounterId",
                schema: "ehr_clinical",
                table: "EncounterDiagnoses",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_PatientId",
                schema: "ehr_clinical",
                table: "Encounters",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_ProviderId_StartedAtUtc",
                schema: "ehr_clinical",
                table: "Encounters",
                columns: new[] { "ProviderId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_export_jobs_Status",
                schema: "fhir_export",
                table: "export_jobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Immunizations_PatientId",
                schema: "ehr_chart",
                table: "Immunizations",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_CompletedAtUtc",
                schema: "ehr",
                table: "InboxMessages",
                column: "CompletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_DeduplicationKey",
                schema: "ehr",
                table: "InboxMessages",
                column: "DeduplicationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsurerTransmissions_ClaimId",
                schema: "ehr_integration",
                table: "InsurerTransmissions",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_InsurerTransmissions_ExternalControlNumber",
                schema: "ehr_integration",
                table: "InsurerTransmissions",
                column: "ExternalControlNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_PatientId",
                schema: "ehr_clinical",
                table: "LabOrders",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_LabOrderId",
                schema: "ehr_clinical",
                table: "LabResults",
                column: "LabOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_PatientId",
                schema: "ehr_clinical",
                table: "LabResults",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_LabTransmissions_ExternalControlNumber",
                schema: "ehr_integration",
                table: "LabTransmissions",
                column: "ExternalControlNumber",
                unique: true,
                filter: "\"ExternalControlNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LabTransmissions_LabOrderId",
                schema: "ehr_integration",
                table: "LabTransmissions",
                column: "LabOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationStatements_PatientId",
                schema: "ehr_chart",
                table: "MedicationStatements",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_outbox_SubscriptionId_DeliveredAt",
                schema: "fhir_subscriptions",
                table: "notification_outbox",
                columns: new[] { "SubscriptionId", "DeliveredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc",
                schema: "ehr",
                table: "OutboxMessages",
                column: "ProcessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_MedicalRecordNumber",
                schema: "ehr_registration",
                table: "Patients",
                column: "MedicalRecordNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payers_PayerCode",
                schema: "ehr_billing",
                table: "Payers",
                column: "PayerCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PatientId",
                schema: "ehr_billing",
                table: "Payments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformedProcedures_EncounterId",
                schema: "ehr_clinical",
                table: "PerformedProcedures",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyTransmissions_ExternalControlNumber",
                schema: "ehr_integration",
                table: "PharmacyTransmissions",
                column: "ExternalControlNumber",
                unique: true,
                filter: "\"ExternalControlNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PharmacyTransmissions_PrescriptionId",
                schema: "ehr_integration",
                table: "PharmacyTransmissions",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PortalAppointmentRequests_PatientId",
                schema: "ehr_portal",
                table: "PortalAppointmentRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_EncounterId",
                schema: "ehr_clinical",
                table: "Prescriptions",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId",
                schema: "ehr_clinical",
                table: "Prescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemListItems_PatientId",
                schema: "ehr_chart",
                table: "ProblemListItems",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderAvailabilityWindows_ProviderId_StartUtc_EndUtc",
                schema: "ehr_scheduling",
                table: "ProviderAvailabilityWindows",
                columns: new[] { "ProviderId", "StartUtc", "EndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_NationalProviderIdentifier",
                schema: "ehr_registration",
                table: "Providers",
                column: "NationalProviderIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Remittances_ClaimId",
                schema: "ehr_billing",
                table: "Remittances",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_SagaInstances_SagaKind_InstanceKey",
                schema: "ehr",
                table: "SagaInstances",
                columns: new[] { "SagaKind", "InstanceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecureMessages_PatientId",
                schema: "ehr_portal",
                table: "SecureMessages",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_SecureMessages_ThreadId",
                schema: "ehr_portal",
                table: "SecureMessages",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_TopicUrl_Status",
                schema: "fhir_subscriptions",
                table: "subscriptions",
                columns: new[] { "TopicUrl", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignReadings_PatientId",
                schema: "ehr_chart",
                table: "VitalSignReadings",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignReadings_PatientId_ObservedAtUtc",
                schema: "ehr_chart",
                table: "VitalSignReadings",
                columns: new[] { "PatientId", "ObservedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Allergies",
                schema: "ehr_chart");

            migrationBuilder.DropTable(
                name: "Appointments",
                schema: "ehr_scheduling");

            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "fhir_audit");

            migrationBuilder.DropTable(
                name: "Charges",
                schema: "ehr_billing");

            migrationBuilder.DropTable(
                name: "Claims",
                schema: "ehr_billing");

            migrationBuilder.DropTable(
                name: "ClinicalNotes",
                schema: "ehr_clinical");

            migrationBuilder.DropTable(
                name: "EncounterDiagnoses",
                schema: "ehr_clinical");

            migrationBuilder.DropTable(
                name: "export_jobs",
                schema: "fhir_export");

            migrationBuilder.DropTable(
                name: "Immunizations",
                schema: "ehr_chart");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "ehr");

            migrationBuilder.DropTable(
                name: "InsurerTransmissions",
                schema: "ehr_integration");

            migrationBuilder.DropTable(
                name: "LabOrders",
                schema: "ehr_clinical");

            migrationBuilder.DropTable(
                name: "LabResults",
                schema: "ehr_clinical");

            migrationBuilder.DropTable(
                name: "LabTransmissions",
                schema: "ehr_integration");

            migrationBuilder.DropTable(
                name: "MedicationStatements",
                schema: "ehr_chart");

            migrationBuilder.DropTable(
                name: "notification_outbox",
                schema: "fhir_subscriptions");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "ehr");

            migrationBuilder.DropTable(
                name: "PatientContactPoints",
                schema: "ehr_registration");

            migrationBuilder.DropTable(
                name: "Payers",
                schema: "ehr_billing");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "ehr_billing");

            migrationBuilder.DropTable(
                name: "PerformedProcedures",
                schema: "ehr_clinical");

            migrationBuilder.DropTable(
                name: "PharmacyTransmissions",
                schema: "ehr_integration");

            migrationBuilder.DropTable(
                name: "PortalAppointmentRequests",
                schema: "ehr_portal");

            migrationBuilder.DropTable(
                name: "Prescriptions",
                schema: "ehr_clinical");

            migrationBuilder.DropTable(
                name: "ProblemListItems",
                schema: "ehr_chart");

            migrationBuilder.DropTable(
                name: "ProviderAvailabilityWindows",
                schema: "ehr_scheduling");

            migrationBuilder.DropTable(
                name: "Providers",
                schema: "ehr_registration");

            migrationBuilder.DropTable(
                name: "Remittances",
                schema: "ehr_billing");

            migrationBuilder.DropTable(
                name: "SagaInstances",
                schema: "ehr");

            migrationBuilder.DropTable(
                name: "SecureMessages",
                schema: "ehr_portal");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "fhir_subscriptions");

            migrationBuilder.DropTable(
                name: "VitalSignReadings",
                schema: "ehr_chart");

            migrationBuilder.DropTable(
                name: "Patients",
                schema: "ehr_registration");

            migrationBuilder.DropTable(
                name: "Encounters",
                schema: "ehr_clinical");
        }
    }
}
