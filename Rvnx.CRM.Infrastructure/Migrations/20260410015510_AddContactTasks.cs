using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddContactTasks : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ContactTask",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ContactId = table.Column<Guid>(type: "TEXT", nullable: true),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: true),
                DueDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ContactTask", x => x.Id);
                table.CheckConstraint("CHK_ContactTask_Owner", "ContactId IS NOT NULL");
                table.ForeignKey(
                    name: "FK_ContactTask_Contact_ContactId",
                    column: x => x.ContactId,
                    principalTable: "Contact",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ContactTask_ContactId",
            table: "ContactTask",
            column: "ContactId");

        migrationBuilder.CreateIndex(
            name: "IX_ContactTask_GroupId",
            table: "ContactTask",
            column: "GroupId");

        migrationBuilder.CreateIndex(
            name: "IX_ContactTask_UserId",
            table: "ContactTask",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ContactTask");
    }
}