#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql.Migrations
{
    /// <inheritdoc />
    public partial class AddEndpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertEvents",
                schema: "smartconnect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ErrorType = table.Column<int>(type: "integer", nullable: false),
                    ErrorDetail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActionOutcomesJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertRules",
                schema: "smartconnect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EnabledFlowIdsJson = table.Column<string>(type: "text", nullable: false),
                    ErrorPatternsJson = table.Column<string>(type: "text", nullable: false),
                    ActionsJson = table.Column<string>(type: "text", nullable: false),
                    ThrottleWindowSeconds = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CasBlobRefs",
                schema: "smartconnect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RefCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CasBlobRefs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DicomInstances",
                schema: "smartconnect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudyInstanceUid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SeriesInstanceUid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SopInstanceUid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SopClassUid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PatientId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PatientName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Modality = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    ReceivedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    BlobId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DicomInstances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Endpoints",
                schema: "smartconnect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ParametersJson = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Endpoints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_FlowId",
                schema: "smartconnect",
                table: "AlertEvents",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_OccurredAtUtc",
                schema: "smartconnect",
                table: "AlertEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_RuleId",
                schema: "smartconnect",
                table: "AlertEvents",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_Enabled",
                schema: "smartconnect",
                table: "AlertRules",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_CasBlobRefs_AttachmentId",
                schema: "smartconnect",
                table: "CasBlobRefs",
                column: "AttachmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CasBlobRefs_ContentHash",
                schema: "smartconnect",
                table: "CasBlobRefs",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_DicomInstances_PatientId_ReceivedUtc",
                schema: "smartconnect",
                table: "DicomInstances",
                columns: new[] { "PatientId", "ReceivedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DicomInstances_SopInstanceUid",
                schema: "smartconnect",
                table: "DicomInstances",
                column: "SopInstanceUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DicomInstances_StudyInstanceUid",
                schema: "smartconnect",
                table: "DicomInstances",
                column: "StudyInstanceUid");

            migrationBuilder.CreateIndex(
                name: "IX_Endpoints_Kind",
                schema: "smartconnect",
                table: "Endpoints",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_Endpoints_Name",
                schema: "smartconnect",
                table: "Endpoints",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertEvents",
                schema: "smartconnect");

            migrationBuilder.DropTable(
                name: "AlertRules",
                schema: "smartconnect");

            migrationBuilder.DropTable(
                name: "CasBlobRefs",
                schema: "smartconnect");

            migrationBuilder.DropTable(
                name: "DicomInstances",
                schema: "smartconnect");

            migrationBuilder.DropTable(
                name: "Endpoints",
                schema: "smartconnect");
        }
    }
}
