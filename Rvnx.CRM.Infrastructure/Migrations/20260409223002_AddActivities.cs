using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddActivities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Activity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: true),
                ActivityDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                ActivityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Activity", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ActivityContact",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ActivityId = table.Column<Guid>(type: "TEXT", nullable: false),
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
                table.PrimaryKey("PK_ActivityContact", x => x.Id);
                table.ForeignKey(
                    name: "FK_ActivityContact_Activity_ActivityId",
                    column: x => x.ActivityId,
                    principalTable: "Activity",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ActivityContact_Contact_ContactId",
                    column: x => x.ContactId,
                    principalTable: "Contact",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Activity_GroupId",
            table: "Activity",
            column: "GroupId");

        migrationBuilder.CreateIndex(
            name: "IX_Activity_UserId",
            table: "Activity",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_ActivityContact_ActivityId_ContactId",
            table: "ActivityContact",

            columns: new[] { "ActivityId", "ContactId" },

            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ActivityContact_ContactId",
            table: "ActivityContact",
            column: "ContactId");

        migrationBuilder.CreateIndex(
            name: "IX_ActivityContact_GroupId",
            table: "ActivityContact",
            column: "GroupId");

        migrationBuilder.CreateIndex(
            name: "IX_ActivityContact_UserId",
            table: "ActivityContact",
            column: "UserId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ActivityContact");

        migrationBuilder.DropTable(
            name: "Activity");
    }
}
