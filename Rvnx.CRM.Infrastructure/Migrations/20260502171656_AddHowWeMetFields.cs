using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHowWeMetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FirstMetOn",
                table: "Contact",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HowWeMet",
                table: "Contact",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IntroducedByContactId",
                table: "Contact",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contact_IntroducedByContactId",
                table: "Contact",
                column: "IntroducedByContactId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contact_Contact_IntroducedByContactId",
                table: "Contact",
                column: "IntroducedByContactId",
                principalTable: "Contact",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contact_Contact_IntroducedByContactId",
                table: "Contact");

            migrationBuilder.DropIndex(
                name: "IX_Contact_IntroducedByContactId",
                table: "Contact");

            migrationBuilder.DropColumn(
                name: "FirstMetOn",
                table: "Contact");

            migrationBuilder.DropColumn(
                name: "HowWeMet",
                table: "Contact");

            migrationBuilder.DropColumn(
                name: "IntroducedByContactId",
                table: "Contact");
        }
    }
}
