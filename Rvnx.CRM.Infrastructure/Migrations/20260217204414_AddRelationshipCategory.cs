using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Rvnx.CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationshipCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "RelationshipType",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("09876543-210f-edcb-a987-6543210fedcb"),
                column: "Category",
                value: "Professional");

            migrationBuilder.UpdateData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("1a2b3c4d-5e6f-7890-a1b2-c3d4e5f67890"),
                column: "Category",
                value: "Professional");

            migrationBuilder.UpdateData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("7c1f8d22-1b6a-4c28-9c1e-3f5a2b8e9d1a"),
                column: "Category",
                value: "Family");

            migrationBuilder.UpdateData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("a5b6c7d8-9e0f-1a2b-3c4d-5e6f7a8b9c0d"),
                column: "Category",
                value: "Social");

            migrationBuilder.UpdateData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("b2e9a5c8-7f4d-4a1b-8c6e-5f9d3a0e2b4c"),
                column: "Category",
                value: "Family");

            migrationBuilder.UpdateData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("d4f1b8a9-3e2c-4b5d-9a6f-1c0e7d8b5a2f"),
                column: "Category",
                value: "Family");

            migrationBuilder.UpdateData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("f9e8d7c6-b5a4-3210-9876-543210fedcba"),
                column: "Category",
                value: "Romantic");

            migrationBuilder.UpdateData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("fedcba98-7654-3210-fedc-ba9876543210"),
                column: "Category",
                value: "Company");

            migrationBuilder.InsertData(
                table: "RelationshipType",
                columns: new[] { "Id", "Category", "CreatedBy", "CreatedDate", "EntityType", "LastChangedBy", "LastChangedDate", "Name", "OppositeName", "UserId" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "Family", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Husband", "Wife", null },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "Family", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Father", "Child", null },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "Family", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mother", "Child", null },
                    { new Guid("11111111-1111-1111-1111-111111111104"), "Family", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Brother", "Sister", null },
                    { new Guid("11111111-1111-1111-1111-111111111105"), "Family", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Brother", "Brother", null },
                    { new Guid("11111111-1111-1111-1111-111111111106"), "Family", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sister", "Sister", null },
                    { new Guid("11111111-1111-1111-1111-111111111107"), "Family", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Grandparent", "Grandchild", null },
                    { new Guid("11111111-1111-1111-1111-111111111108"), "Family", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Uncle/Aunt", "Nephew/Niece", null },
                    { new Guid("11111111-1111-1111-1111-111111111109"), "Family", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cousin", "Cousin", null },
                    { new Guid("22222222-2222-2222-2222-222222222201"), "Romantic", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Boyfriend", "Girlfriend", null },
                    { new Guid("22222222-2222-2222-2222-222222222202"), "Romantic", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Boyfriend", "Boyfriend", null },
                    { new Guid("22222222-2222-2222-2222-222222222203"), "Romantic", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Girlfriend", "Girlfriend", null },
                    { new Guid("33333333-3333-3333-3333-333333333301"), "Professional", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Colleague", "Colleague", null },
                    { new Guid("33333333-3333-3333-3333-333333333302"), "Professional", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Business Partner", "Business Partner", null },
                    { new Guid("44444444-4444-4444-4444-444444444401"), "Social", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Person", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Acquaintance", "Acquaintance", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333301"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333302"));

            migrationBuilder.DeleteData(
                table: "RelationshipType",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444401"));

            migrationBuilder.DropColumn(
                name: "Category",
                table: "RelationshipType");
        }
    }
}
