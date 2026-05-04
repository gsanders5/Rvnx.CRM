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

            // Strip stray time components from DateOnly text columns. Some legacy rows were
            // persisted as 'YYYY-MM-DD HH:MM:SS' which SqliteValueReader.GetDateOnly rejects.
            migrationBuilder.Sql(
                "UPDATE Pet SET Birthday = substr(Birthday, 1, 10) WHERE Birthday LIKE '% %';");
            migrationBuilder.Sql(
                "UPDATE Relationship SET StartDate = substr(StartDate, 1, 10) WHERE StartDate LIKE '% %';");
            migrationBuilder.Sql(
                "UPDATE Relationship SET EndDate = substr(EndDate, 1, 10) WHERE EndDate LIKE '% %';");
            migrationBuilder.Sql(
                "UPDATE Contact SET DateOfDeath = substr(DateOfDeath, 1, 10) WHERE DateOfDeath LIKE '% %';");
            migrationBuilder.Sql(
                "UPDATE Contact SET FirstMetOn = substr(FirstMetOn, 1, 10) WHERE FirstMetOn LIKE '% %';");
            migrationBuilder.Sql(
                "UPDATE SignificantDate SET EventDate = substr(EventDate, 1, 10) WHERE EventDate LIKE '% %';");
            migrationBuilder.Sql(
                "UPDATE ContactTask SET DueDate = substr(DueDate, 1, 10) WHERE DueDate LIKE '% %';");
            migrationBuilder.Sql(
                "UPDATE ReminderLog SET OccurrenceDate = substr(OccurrenceDate, 1, 10) WHERE OccurrenceDate LIKE '% %';");
            migrationBuilder.Sql(
                "UPDATE ReminderLog SET ScheduledFor = substr(ScheduledFor, 1, 10) WHERE ScheduledFor LIKE '% %';");
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
