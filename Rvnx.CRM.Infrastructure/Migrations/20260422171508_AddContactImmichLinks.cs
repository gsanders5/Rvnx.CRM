using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContactImmichLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactImmichLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImmichPersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ImmichPersonName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ImmichTagId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ImmichTagValue = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactImmichLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactImmichLinks_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactImmichLinks_ContactId",
                table: "ContactImmichLinks",
                column: "ContactId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactImmichLinks_GroupId",
                table: "ContactImmichLinks",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactImmichLinks_UserId",
                table: "ContactImmichLinks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactImmichLinks");
        }
    }
}
