using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSelfContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LinkedUserId",
                table: "Contact",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contact_LinkedUserId",
                table: "Contact",
                column: "LinkedUserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Contact_Users_LinkedUserId",
                table: "Contact",
                column: "LinkedUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contact_Users_LinkedUserId",
                table: "Contact");

            migrationBuilder.DropIndex(
                name: "IX_Contact_LinkedUserId",
                table: "Contact");

            migrationBuilder.DropColumn(
                name: "LinkedUserId",
                table: "Contact");
        }
    }
}
