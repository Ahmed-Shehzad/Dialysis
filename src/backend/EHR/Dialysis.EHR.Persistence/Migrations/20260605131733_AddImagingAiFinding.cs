using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.EHR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImagingAiFinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiFindingCode",
                schema: "ehr_clinical",
                table: "ImagingOrders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AiFindingConfidence",
                schema: "ehr_clinical",
                table: "ImagingOrders",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiFindingDisplay",
                schema: "ehr_clinical",
                table: "ImagingOrders",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiFindingInterpretation",
                schema: "ehr_clinical",
                table: "ImagingOrders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AiFindingStatus",
                schema: "ehr_clinical",
                table: "ImagingOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AiFindingSummary",
                schema: "ehr_clinical",
                table: "ImagingOrders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiFindingSystem",
                schema: "ehr_clinical",
                table: "ImagingOrders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiModelId",
                schema: "ehr_clinical",
                table: "ImagingOrders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AiReviewedAtUtc",
                schema: "ehr_clinical",
                table: "ImagingOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiReviewedBy",
                schema: "ehr_clinical",
                table: "ImagingOrders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiFindingCode",
                schema: "ehr_clinical",
                table: "ImagingOrders");

            migrationBuilder.DropColumn(
                name: "AiFindingConfidence",
                schema: "ehr_clinical",
                table: "ImagingOrders");

            migrationBuilder.DropColumn(
                name: "AiFindingDisplay",
                schema: "ehr_clinical",
                table: "ImagingOrders");

            migrationBuilder.DropColumn(
                name: "AiFindingInterpretation",
                schema: "ehr_clinical",
                table: "ImagingOrders");

            migrationBuilder.DropColumn(
                name: "AiFindingStatus",
                schema: "ehr_clinical",
                table: "ImagingOrders");

            migrationBuilder.DropColumn(
                name: "AiFindingSummary",
                schema: "ehr_clinical",
                table: "ImagingOrders");

            migrationBuilder.DropColumn(
                name: "AiFindingSystem",
                schema: "ehr_clinical",
                table: "ImagingOrders");

            migrationBuilder.DropColumn(
                name: "AiModelId",
                schema: "ehr_clinical",
                table: "ImagingOrders");

            migrationBuilder.DropColumn(
                name: "AiReviewedAtUtc",
                schema: "ehr_clinical",
                table: "ImagingOrders");

            migrationBuilder.DropColumn(
                name: "AiReviewedBy",
                schema: "ehr_clinical",
                table: "ImagingOrders");
        }
    }
}
