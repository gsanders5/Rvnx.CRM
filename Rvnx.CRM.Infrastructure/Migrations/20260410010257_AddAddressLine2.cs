using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddAddressLine2 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Street",
            table: "Address",
            newName: "Line1");

        migrationBuilder.AddColumn<string>(
            name: "Line2",
            table: "Address",
            type: "TEXT",
            maxLength: 200,
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Line2",
            table: "Address");

        migrationBuilder.RenameColumn(
            name: "Line1",
            table: "Address",
            newName: "Street");
    }
}