#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore.Postgresql.Migrations
{
    /// <inheritdoc />
    public partial class AddTransponderInboxAndOutboxCorrelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                schema: "transponder",
                table: "OutboxMessages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "transponder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeduplicationKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RoutingKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "transponder");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "transponder",
                table: "OutboxMessages");
        }
    }
}
