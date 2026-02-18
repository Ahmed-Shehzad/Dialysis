using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Prescription.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPrescriptionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    OrderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PatientMrn = table.Column<string>(type: "text", nullable: false),
                    Modality = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OrderingProvider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CallbackPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SettingsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_Prescriptions", x => x.Id);
                });

            _ = migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_OrderId",
                table: "Prescriptions",
                column: "OrderId",
                unique: true);

            _ = migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientMrn",
                table: "Prescriptions",
                column: "PatientMrn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropTable(
                name: "Prescriptions");
        }
    }
}
