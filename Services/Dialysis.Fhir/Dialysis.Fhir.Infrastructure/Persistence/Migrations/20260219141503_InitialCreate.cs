using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Fhir.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "default"),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChannelType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Endpoint = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Criteria = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ResourceJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_Status",
                table: "Subscriptions",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subscriptions");
        }
    }
}
