using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyRelationshipTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Relationship_EntityId_EntityType",
                table: "Relationship");

            migrationBuilder.DropIndex(
                name: "IX_Relationship_RelatedEntityId_EntityType",
                table: "Relationship");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "Relationship");

            migrationBuilder.RenameColumn(
                name: "RelatedEntityId",
                table: "Relationship",
                newName: "RelatedContactId");

            migrationBuilder.RenameColumn(
                name: "EntityId",
                table: "Relationship",
                newName: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationship_ContactId",
                table: "Relationship",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Relationship_RelatedContactId",
                table: "Relationship",
                column: "RelatedContactId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Relationship_ContactId",
                table: "Relationship");

            migrationBuilder.DropIndex(
                name: "IX_Relationship_RelatedContactId",
                table: "Relationship");

            migrationBuilder.RenameColumn(
                name: "RelatedContactId",
                table: "Relationship",
                newName: "RelatedEntityId");

            migrationBuilder.RenameColumn(
                name: "ContactId",
                table: "Relationship",
                newName: "EntityId");

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "Relationship",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Relationship_EntityId_EntityType",
                table: "Relationship",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_Relationship_RelatedEntityId_EntityType",
                table: "Relationship",
                columns: new[] { "RelatedEntityId", "EntityType" });
        }
    }
}
