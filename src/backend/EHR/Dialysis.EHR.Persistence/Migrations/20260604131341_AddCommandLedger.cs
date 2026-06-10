#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.EHR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommandLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ehr_durablecommands");

            migrationBuilder.CreateTable(
                name: "command_ledger",
                schema: "ehr_durablecommands",
                columns: table => new
                {
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandTypeKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EnqueuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: true),
                    FailureJson = table.Column<string>(type: "text", nullable: true),
                    RequestedBySubject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ConsumerInstanceId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_command_ledger", x => x.CommandId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_command_ledger_CorrelationId",
                schema: "ehr_durablecommands",
                table: "command_ledger",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_command_ledger_Status_AppliedAtUtc",
                schema: "ehr_durablecommands",
                table: "command_ledger",
                columns: new[] { "Status", "AppliedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "command_ledger",
                schema: "ehr_durablecommands");
        }
    }
}
