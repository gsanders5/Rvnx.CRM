using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddContactFavorites : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ContactFavorite",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ContactId = table.Column<Guid>(type: "TEXT", nullable: false),
                CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContactFavorite", x => x.Id);
                table.ForeignKey(
                    name: "FK_ContactFavorite_Contact_ContactId",
                    column: x => x.ContactId,
                    principalTable: "Contact",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ContactFavorite_ContactId",
            table: "ContactFavorite",
            column: "ContactId");

        migrationBuilder.CreateIndex(
            name: "IX_ContactFavorite_GroupId",
            table: "ContactFavorite",
            column: "GroupId");

        migrationBuilder.CreateIndex(
            name: "IX_ContactFavorite_UserId",
            table: "ContactFavorite",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_ContactFavorite_UserId_ContactId",
            table: "ContactFavorite",
#pragma warning disable CA1861
            columns: new[] { "UserId", "ContactId" },
#pragma warning restore CA1861
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ContactFavorite");
    }
}