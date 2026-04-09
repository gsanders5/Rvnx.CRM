using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations;

/// <inheritdoc />
public partial class PetMultipleOwners : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Create the PetContact join table first (before dropping ContactId)
        migrationBuilder.CreateTable(
            name: "PetContact",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                PetId = table.Column<Guid>(type: "TEXT", nullable: false),
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
                table.PrimaryKey("PK_PetContact", x => x.Id);
                table.ForeignKey(
                    name: "FK_PetContact_Contact_ContactId",
                    column: x => x.ContactId,
                    principalTable: "Contact",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PetContact_Pet_PetId",
                    column: x => x.PetId,
                    principalTable: "Pet",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PetContact_ContactId",
            table: "PetContact",
            column: "ContactId");

        migrationBuilder.CreateIndex(
            name: "IX_PetContact_GroupId",
            table: "PetContact",
            column: "GroupId");

        migrationBuilder.CreateIndex(
            name: "IX_PetContact_PetId_ContactId",
            table: "PetContact",
#pragma warning disable CA1861
            columns: new[] { "PetId", "ContactId" },
#pragma warning restore CA1861
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PetContact_UserId",
            table: "PetContact",
            column: "UserId");

        // 2. Migrate existing Pet → Contact relationships into PetContact
        migrationBuilder.Sql(@"
                INSERT INTO PetContact (Id, PetId, ContactId, CreatedBy, LastChangedBy, CreatedDate, LastChangedDate, UserId, GroupId)
                SELECT
                    lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || substr(hex(randomblob(2)),2) || '-' || substr('89ab', abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)),2) || '-' || hex(randomblob(6))),
                    p.Id,
                    p.ContactId,
                    p.CreatedBy,
                    p.LastChangedBy,
                    p.CreatedDate,
                    p.LastChangedDate,
                    p.UserId,
                    p.GroupId
                FROM Pet p
                WHERE p.ContactId IS NOT NULL
                  AND p.ContactId != '00000000-0000-0000-0000-000000000000';
            ");

        // 3. Now drop the old FK, index, and column
        migrationBuilder.DropForeignKey(
            name: "FK_Pet_Contact_ContactId",
            table: "Pet");

        migrationBuilder.DropIndex(
            name: "IX_Pet_ContactId",
            table: "Pet");

        migrationBuilder.DropColumn(
            name: "ContactId",
            table: "Pet");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ContactId",
            table: "Pet",
            type: "TEXT",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        // Migrate data back: take one ContactId per Pet from PetContact
        migrationBuilder.Sql(@"
                UPDATE Pet
                SET ContactId = (
                    SELECT pc.ContactId
                    FROM PetContact pc
                    WHERE pc.PetId = Pet.Id
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1 FROM PetContact pc WHERE pc.PetId = Pet.Id
                );
            ");

        migrationBuilder.DropTable(
            name: "PetContact");

        migrationBuilder.CreateIndex(
            name: "IX_Pet_ContactId",
            table: "Pet",
            column: "ContactId");

        migrationBuilder.AddForeignKey(
            name: "FK_Pet_Contact_ContactId",
            table: "Pet",
            column: "ContactId",
            principalTable: "Contact",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}