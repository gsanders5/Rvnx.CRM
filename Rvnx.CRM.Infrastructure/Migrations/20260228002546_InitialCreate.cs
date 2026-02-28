using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rvnx.CRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1861:Prefer 'static readonly' fields over constant array arguments", Justification = "EF Core Migrations are auto-generated and use inline arrays for column definitions.")]
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contact",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsPartial = table.Column<bool>(type: "INTEGER", nullable: false),
                    Pronouns = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Religion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Nickname = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Company = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsHidden = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Label",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Label", x => x.Id);
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
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationship", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Street = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Zip = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AddressType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                    table.CheckConstraint("CHK_Address_Owner", "ContactId IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Address_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attachment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AttachmentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachment", x => x.Id);
                    table.CheckConstraint("CHK_Attachment_Owner", "ContactId IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Attachment_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactMethod",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactMethod", x => x.Id);
                    table.CheckConstraint("CHK_ContactMethod_Owner", "ContactId IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_ContactMethod_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
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
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true),
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
                name: "Fact",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fact", x => x.Id);
                    table.CheckConstraint("CHK_Fact_Owner", "ContactId IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Fact_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Note",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Note", x => x.Id);
                    table.CheckConstraint("CHK_Note_Owner", "ContactId IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Note_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Species = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Breed = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Birthday = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pet_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhoneNumber",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", maxLength: 20, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneNumber", x => x.Id);
                    table.CheckConstraint("CHK_PhoneNumber_Owner", "ContactId IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_PhoneNumber_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SignificantDate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    EventDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    RecurrenceType = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomIntervalDays = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignificantDate", x => x.Id);
                    table.CheckConstraint("CHK_SignificantDate_Owner", "ContactId IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_SignificantDate_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContactLabel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LabelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactLabel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactLabel_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContactLabel_Label_LabelId",
                        column: x => x.LabelId,
                        principalTable: "Label",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    SubjectId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    SelfContactId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsAdministrator = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Contact_SelfContactId",
                        column: x => x.SelfContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_UserGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "UserGroups",
                        principalColumn: "Id");
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
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
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
                name: "ReminderOffset",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SignificantDateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DaysBeforeEvent = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderOffset", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReminderOffset_SignificantDate_SignificantDateId",
                        column: x => x.SignificantDateId,
                        principalTable: "SignificantDate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReminderLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReminderOffsetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OccurrenceDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ScheduledFor = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    LastChangedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastChangedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", maxLength: 450, nullable: true),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReminderLog_ReminderOffset_ReminderOffsetId",
                        column: x => x.ReminderOffsetId,
                        principalTable: "ReminderOffset",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Address_ContactId",
                table: "Address",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Address_GroupId",
                table: "Address",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Address_UserId",
                table: "Address",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_ContactId",
                table: "Attachment",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_GroupId",
                table: "Attachment",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_UserId",
                table: "Attachment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentContent_AttachmentId",
                table: "AttachmentContent",
                column: "AttachmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentContent_GroupId",
                table: "AttachmentContent",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AttachmentContent_UserId",
                table: "AttachmentContent",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_GroupId",
                table: "Contact",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Contact_UserId",
                table: "Contact",
                column: "UserId");


            migrationBuilder.CreateIndex(
                name: "IX_ContactLabel_ContactId_LabelId",
                table: "ContactLabel",
                columns: new[] { "ContactId", "LabelId" },
                unique: true);


            migrationBuilder.CreateIndex(
                name: "IX_ContactLabel_GroupId",
                table: "ContactLabel",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactLabel_LabelId",
                table: "ContactLabel",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactLabel_UserId",
                table: "ContactLabel",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMethod_ContactId",
                table: "ContactMethod",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMethod_GroupId",
                table: "ContactMethod",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactMethod_UserId",
                table: "ContactMethod",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Employer_EmployeeId",
                table: "Employer",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employer_GroupId",
                table: "Employer",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Employer_UserId",
                table: "Employer",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Fact_ContactId",
                table: "Fact",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Fact_GroupId",
                table: "Fact",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Fact_UserId",
                table: "Fact",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Label_GroupId",
                table: "Label",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Label_UserId",
                table: "Label",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Note_ContactId",
                table: "Note",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Note_GroupId",
                table: "Note",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Note_UserId",
                table: "Note",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Pet_ContactId",
                table: "Pet",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_Pet_GroupId",
                table: "Pet",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Pet_UserId",
                table: "Pet",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumber_ContactId",
                table: "PhoneNumber",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumber_GroupId",
                table: "PhoneNumber",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneNumber_UserId",
                table: "PhoneNumber",
                column: "UserId");


            migrationBuilder.CreateIndex(
                name: "IX_Relationship_EntityId_EntityType",
                table: "Relationship",
                columns: new[] { "EntityId", "EntityType" });


            migrationBuilder.CreateIndex(
                name: "IX_Relationship_GroupId",
                table: "Relationship",
                column: "GroupId");


            migrationBuilder.CreateIndex(
                name: "IX_Relationship_RelatedEntityId_EntityType",
                table: "Relationship",
                columns: new[] { "RelatedEntityId", "EntityType" });


            migrationBuilder.CreateIndex(
                name: "IX_Relationship_UserId",
                table: "Relationship",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderLog_GroupId",
                table: "ReminderLog",
                column: "GroupId");


            migrationBuilder.CreateIndex(
                name: "IX_ReminderLog_ReminderOffsetId_OccurrenceDate",
                table: "ReminderLog",
                columns: new[] { "ReminderOffsetId", "OccurrenceDate" },
                unique: true);


            migrationBuilder.CreateIndex(
                name: "IX_ReminderLog_UserId",
                table: "ReminderLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderOffset_GroupId",
                table: "ReminderOffset",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderOffset_SignificantDateId",
                table: "ReminderOffset",
                column: "SignificantDateId");

            migrationBuilder.CreateIndex(
                name: "IX_ReminderOffset_UserId",
                table: "ReminderOffset",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SignificantDate_ContactId",
                table: "SignificantDate",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_SignificantDate_GroupId",
                table: "SignificantDate",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SignificantDate_UserId",
                table: "SignificantDate",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_GroupId",
                table: "UserGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_UserId",
                table: "UserGroups",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GroupId",
                table: "Users",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SelfContactId",
                table: "Users",
                column: "SelfContactId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserId",
                table: "Users",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.DropTable(
                name: "AttachmentContent");

            migrationBuilder.DropTable(
                name: "ContactLabel");

            migrationBuilder.DropTable(
                name: "ContactMethod");

            migrationBuilder.DropTable(
                name: "Employer");

            migrationBuilder.DropTable(
                name: "Fact");

            migrationBuilder.DropTable(
                name: "Note");

            migrationBuilder.DropTable(
                name: "Pet");

            migrationBuilder.DropTable(
                name: "PhoneNumber");

            migrationBuilder.DropTable(
                name: "Relationship");

            migrationBuilder.DropTable(
                name: "ReminderLog");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Attachment");

            migrationBuilder.DropTable(
                name: "Label");

            migrationBuilder.DropTable(
                name: "ReminderOffset");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "SignificantDate");

            migrationBuilder.DropTable(
                name: "Contact");
        }
    }
}
