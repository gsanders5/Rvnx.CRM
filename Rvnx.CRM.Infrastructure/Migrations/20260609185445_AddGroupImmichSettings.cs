using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupImmichSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupImmichSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    BaseUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    ApiKey = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupImmichSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupImmichSettings_GroupId",
                table: "GroupImmichSettings",
                column: "GroupId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupImmichSettings_UserId",
                table: "GroupImmichSettings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupImmichSettings");
        }
    }
}
