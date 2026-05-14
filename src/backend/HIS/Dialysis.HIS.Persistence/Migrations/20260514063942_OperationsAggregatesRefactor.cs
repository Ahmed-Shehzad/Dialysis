using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.HIS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OperationsAggregatesRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "his_operations",
                table: "StaffMembers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "his_operations",
                table: "StaffMembers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "his_operations",
                table: "StaffMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "his_operations",
                table: "StaffMembers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "his_operations",
                table: "StaffMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "his_operations",
                table: "StaffMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "his_operations",
                table: "StaffMembers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "his_operations",
                table: "InventoryItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "his_operations",
                table: "InventoryItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "his_operations",
                table: "InventoryItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "his_operations",
                table: "InventoryItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "his_operations",
                table: "InventoryItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "his_operations",
                table: "InventoryItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "his_operations",
                table: "InventoryItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "his_operations",
                table: "BillingExportJobs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "his_operations",
                table: "BillingExportJobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "his_operations",
                table: "BillingExportJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "his_operations",
                table: "BillingExportJobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "his_operations",
                table: "BillingExportJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "his_operations",
                table: "BillingExportJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "his_operations",
                table: "BillingExportJobs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "his_operations",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "his_operations",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "his_operations",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "his_operations",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "his_operations",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "his_operations",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "his_operations",
                table: "StaffMembers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "his_operations",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "his_operations",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "his_operations",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "his_operations",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "his_operations",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "his_operations",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "his_operations",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "his_operations",
                table: "BillingExportJobs");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "his_operations",
                table: "BillingExportJobs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "his_operations",
                table: "BillingExportJobs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "his_operations",
                table: "BillingExportJobs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "his_operations",
                table: "BillingExportJobs");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "his_operations",
                table: "BillingExportJobs");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "his_operations",
                table: "BillingExportJobs");
        }
    }
}
