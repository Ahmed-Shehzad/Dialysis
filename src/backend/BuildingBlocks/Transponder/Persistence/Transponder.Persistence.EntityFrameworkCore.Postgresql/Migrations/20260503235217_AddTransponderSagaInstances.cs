#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore.Postgresql.Migrations
{
    /// <inheritdoc />
    public partial class AddTransponderSagaInstances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "SagaInstances",
                schema: "transponder");
        }
    }
}
