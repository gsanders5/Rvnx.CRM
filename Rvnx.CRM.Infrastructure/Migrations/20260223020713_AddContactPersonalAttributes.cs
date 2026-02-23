using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContactPersonalAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Contact",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pronouns",
                table: "Contact",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Religion",
                table: "Contact",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Contact");

            migrationBuilder.DropColumn(
                name: "Pronouns",
                table: "Contact");

            migrationBuilder.DropColumn(
                name: "Religion",
                table: "Contact");
        }
    }
}
