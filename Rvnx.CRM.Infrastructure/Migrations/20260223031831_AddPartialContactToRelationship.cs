using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartialContactToRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "RelatedEntityId",
                table: "Relationship",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<bool>(
                name: "IsTypeReverse",
                table: "Relationship",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PartialContactDateOfBirth",
                table: "Relationship",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartialContactFirstName",
                table: "Relationship",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartialContactLastName",
                table: "Relationship",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTypeReverse",
                table: "Relationship");

            migrationBuilder.DropColumn(
                name: "PartialContactDateOfBirth",
                table: "Relationship");

            migrationBuilder.DropColumn(
                name: "PartialContactFirstName",
                table: "Relationship");

            migrationBuilder.DropColumn(
                name: "PartialContactLastName",
                table: "Relationship");

            migrationBuilder.AlterColumn<Guid>(
                name: "RelatedEntityId",
                table: "Relationship",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
