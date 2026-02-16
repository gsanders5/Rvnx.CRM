using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSelfContactToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SelfContactId",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SelfContactId",
                table: "Users",
                column: "SelfContactId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Contact_SelfContactId",
                table: "Users",
                column: "SelfContactId",
                principalTable: "Contact",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Contact_SelfContactId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SelfContactId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SelfContactId",
                table: "Users");
        }
    }
}
