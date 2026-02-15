using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Rvnx.CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attachment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AttachmentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contact",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Nickname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Company = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactInfo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fact",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fact", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportantDate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportantDate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Note",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Note", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Species = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Breed = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Birthday = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhoneNumber",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", maxLength: 20, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneNumber", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RelationshipType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OppositeName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "Reminder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminder", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttachmentContent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<byte[]>(type: "BLOB", nullable: false),
                    AttachmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentContent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttachmentContent_Attachment_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Employer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JobTitle = table.Column<string>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Website = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employer_Contact_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Relationship",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelationshipTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationship", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Relationship_RelationshipType_RelationshipTypeId",
                        column: x => x.RelationshipTypeId,
                        principalTable: "RelationshipType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RelationshipType",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "EntityType", "LastChangedBy", "LastChangedDate", "Name", "OppositeName", "UserId" },
                values: new object[,]
                {
                    { new Guid("09876543-210f-edcb-a987-6543210fedcb"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Teacher", "Student", null },
                    { new Guid("1a2b3c4d-5e6f-7890-a1b2-c3d4e5f67890"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manager", "Employee", null },
                    { new Guid("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Parent", "Child", null },
                    { new Guid("a5b6c7d8-9e0f-1a2b-3c4d-5e6f7a8b9c0d"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Friend", "Friend", null },
                    { new Guid("b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Spouse", "Spouse", null },
                    { new Guid("d4f1b8a9-3e2c-4b5d-9a6f-1c0e7d8b5a2f"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sibling", "Sibling", null },
                    { new Guid("f9e8d7c6-b5a4-3210-9876-543210fedcba"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Partner", "Partner", null },
                    { new Guid("fedcba98-7654-3210-fedc-ba9876543210"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Company", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Parent Company", "Subsidiary", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_EntityId_EntityType",
                table: "Attachment",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentContent_AttachmentId",
                table: "AttachmentContent",
                column: "AttachmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactInfo_EntityId_EntityType",
                table: "ContactInfo",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_Employer_EmployeeId",
                table: "Employer",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Fact_EntityId_EntityType",
                table: "Fact",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportantDate_EntityId_EntityType",
                table: "ImportantDate",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_Note_EntityId_EntityType",
                table: "Note",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_Pet_EntityId_EntityType",
                table: "Pet",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumber_EntityId_EntityType",
                table: "PhoneNumber",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_Relationship_EntityId_EntityType",
                table: "Relationship",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_Relationship_RelatedEntityId_EntityType",
                table: "Relationship",
                columns: new[] { "RelatedEntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_Relationship_RelationshipTypeId",
                table: "Relationship",
                column: "RelationshipTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminder_EntityId_EntityType",
                table: "Reminder",
                columns: new[] { "EntityId", "EntityType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttachmentContent");

            migrationBuilder.DropTable(
                name: "ContactInfo");

            migrationBuilder.DropTable(
                name: "Employer");

            migrationBuilder.DropTable(
                name: "Fact");

            migrationBuilder.DropTable(
                name: "ImportantDate");

            migrationBuilder.DropTable(
                name: "Note");

            migrationBuilder.DropTable(
                name: "Pet");

            migrationBuilder.DropTable(
                name: "PhoneNumber");

            migrationBuilder.DropTable(
                name: "Relationship");

            migrationBuilder.DropTable(
                name: "Reminder");

            migrationBuilder.DropTable(
                name: "Attachment");

            migrationBuilder.DropTable(
                name: "Contact");

            migrationBuilder.DropTable(
                name: "RelationshipType");
        }
    }
}
