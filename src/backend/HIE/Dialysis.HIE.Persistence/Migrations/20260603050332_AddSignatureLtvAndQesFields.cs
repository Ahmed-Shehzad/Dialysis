using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.HIE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSignatureLtvAndQesFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PadesLevel",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RevocationEvidenceBlob",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RevocationEvidenceFormat",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SignatureFormat",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TimestampedAtUtc",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TsaCertThumbprint",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TsaUri",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TspCredentialId",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TspId",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PadesLevel",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures");

            migrationBuilder.DropColumn(
                name: "RevocationEvidenceBlob",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures");

            migrationBuilder.DropColumn(
                name: "RevocationEvidenceFormat",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures");

            migrationBuilder.DropColumn(
                name: "SignatureFormat",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures");

            migrationBuilder.DropColumn(
                name: "TimestampedAtUtc",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures");

            migrationBuilder.DropColumn(
                name: "TsaCertThumbprint",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures");

            migrationBuilder.DropColumn(
                name: "TsaUri",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures");

            migrationBuilder.DropColumn(
                name: "TspCredentialId",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures");

            migrationBuilder.DropColumn(
                name: "TspId",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures");
        }
    }
}
