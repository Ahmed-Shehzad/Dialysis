#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.EHR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCptFeeScheduleAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAtUtc",
                schema: "ehr_billing",
                table: "Claims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayerClaimControlNumber",
                schema: "ehr_billing",
                table: "Claims",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChargeIdempotencyMarkers",
                schema: "ehr_billing",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CptCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ChargeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargeIdempotencyMarkers", x => new { x.SessionId, x.CptCode });
                });

            migrationBuilder.CreateTable(
                name: "ClaimAcknowledgement",
                schema: "ehr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Verdict = table.Column<int>(type: "integer", nullable: false),
                    PayerClaimControlNumber = table.Column<string>(type: "text", nullable: true),
                    ReasonCodes = table.Column<string[]>(type: "text[]", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimAcknowledgement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimAcknowledgement_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalSchema: "ehr_billing",
                        principalTable: "Claims",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CptFeeSchedule",
                schema: "ehr_billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CptCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    PayerCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    EffectiveFromUtc = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveUntilUtc = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CptFeeSchedule", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChargeIdempotencyMarkers_ChargeId",
                schema: "ehr_billing",
                table: "ChargeIdempotencyMarkers",
                column: "ChargeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimAcknowledgement_ClaimId",
                schema: "ehr",
                table: "ClaimAcknowledgement",
                column: "ClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_CptFeeSchedule_CptCode_PayerCode_EffectiveFromUtc",
                schema: "ehr_billing",
                table: "CptFeeSchedule",
                columns: new[] { "CptCode", "PayerCode", "EffectiveFromUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChargeIdempotencyMarkers",
                schema: "ehr_billing");

            migrationBuilder.DropTable(
                name: "ClaimAcknowledgement",
                schema: "ehr");

            migrationBuilder.DropTable(
                name: "CptFeeSchedule",
                schema: "ehr_billing");

            migrationBuilder.DropColumn(
                name: "AcknowledgedAtUtc",
                schema: "ehr_billing",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "PayerClaimControlNumber",
                schema: "ehr_billing",
                table: "Claims");
        }
    }
}
