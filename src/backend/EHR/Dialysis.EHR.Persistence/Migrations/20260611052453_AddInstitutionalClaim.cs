using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.EHR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionalClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InstitutionalAdmissionDateUtc",
                schema: "ehr_billing",
                table: "Claims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstitutionalAdmissionTypeCode",
                schema: "ehr_billing",
                table: "Claims",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstitutionalOtherProcedureCodes",
                schema: "ehr_billing",
                table: "Claims",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstitutionalPrincipalProcedureCode",
                schema: "ehr_billing",
                table: "Claims",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InstitutionalStatementFromUtc",
                schema: "ehr_billing",
                table: "Claims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InstitutionalStatementToUtc",
                schema: "ehr_billing",
                table: "Claims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstitutionalTypeOfBill",
                schema: "ehr_billing",
                table: "Claims",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevenueCode",
                schema: "ehr_billing",
                table: "Charges",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstitutionalAdmissionDateUtc",
                schema: "ehr_billing",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "InstitutionalAdmissionTypeCode",
                schema: "ehr_billing",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "InstitutionalOtherProcedureCodes",
                schema: "ehr_billing",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "InstitutionalPrincipalProcedureCode",
                schema: "ehr_billing",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "InstitutionalStatementFromUtc",
                schema: "ehr_billing",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "InstitutionalStatementToUtc",
                schema: "ehr_billing",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "InstitutionalTypeOfBill",
                schema: "ehr_billing",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "RevenueCode",
                schema: "ehr_billing",
                table: "Charges");
        }
    }
}
