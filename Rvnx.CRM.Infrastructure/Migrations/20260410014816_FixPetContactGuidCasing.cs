using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPetContactGuidCasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix PetContact IDs created by PetMultipleOwners migration with lowercase GUIDs.
            // EF Core SQLite stores GUIDs as uppercase TEXT, and SQLite comparison is case-sensitive,
            // so the lowercase IDs cause DbUpdateConcurrencyException on delete.
            migrationBuilder.Sql("UPDATE PetContact SET Id = upper(Id) WHERE Id != upper(Id);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
