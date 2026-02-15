using Microsoft.EntityFrameworkCore.Migrations;

namespace Transponder.Persistence.EntityFramework.PostgreSql.Migrations;

public partial class AddSagaStateVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        string? schema = null;
        const string sagaTable = "SagaStates";

        _ = migrationBuilder.AddColumn<int>(
            name: "Version",
            schema: schema,
            table: sagaTable,
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        string? schema = null;

        _ = migrationBuilder.DropColumn(
            name: "Version",
            schema: schema,
            table: "SagaStates");
    }
}
