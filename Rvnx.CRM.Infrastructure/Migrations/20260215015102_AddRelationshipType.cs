using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Rvnx.CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationshipType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Relationship");

            migrationBuilder.AddColumn<Guid>(
                name: "RelationshipTypeId",
                table: "Relationship",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "RelationshipType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OppositeName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationshipType", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "RelationshipType",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "LastChangedBy", "LastChangedDate", "Name", "OppositeName", "UserId" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Parent", "Child", null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Spouse", "Spouse", null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sibling", "Sibling", null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Friend", "Friend", null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Partner", "Partner", null },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manager", "Employee", null },
                    { new Guid("77777777-7777-7777-7777-777777777777"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Teacher", "Student", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Relationship_RelationshipTypeId",
                table: "Relationship",
                column: "RelationshipTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Relationship_RelationshipType_RelationshipTypeId",
                table: "Relationship",
                column: "RelationshipTypeId",
                principalTable: "RelationshipType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Relationship_RelationshipType_RelationshipTypeId",
                table: "Relationship");

            migrationBuilder.DropTable(
                name: "RelationshipType");

            migrationBuilder.DropIndex(
                name: "IX_Relationship_RelationshipTypeId",
                table: "Relationship");

            migrationBuilder.DropColumn(
                name: "RelationshipTypeId",
                table: "Relationship");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Relationship",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
