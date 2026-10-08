using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Share.Web.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IngestRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    FileNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecordsInFile = table.Column<int>(type: "INTEGER", nullable: false),
                    Added = table.Column<int>(type: "INTEGER", nullable: false),
                    AlreadyPresent = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkippedRow",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IngestRunId = table.Column<int>(type: "INTEGER", nullable: false),
                    RowNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkippedRow", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkippedRow_IngestRuns_IngestRunId",
                        column: x => x.IngestRunId,
                        principalTable: "IngestRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisaApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubmissionGuid = table.Column<string>(type: "TEXT", nullable: false),
                    Uan = table.Column<string>(type: "TEXT", nullable: false),
                    Gwf = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    SponsorGivenName = table.Column<string>(type: "TEXT", nullable: true),
                    SponsorFamilyName = table.Column<string>(type: "TEXT", nullable: true),
                    Council = table.Column<string>(type: "TEXT", nullable: true),
                    IngestRunId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisaApplications_IngestRuns_IngestRunId",
                        column: x => x.IngestRunId,
                        principalTable: "IngestRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationPeople",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VisaApplicationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: true),
                    GivenName = table.Column<string>(type: "TEXT", nullable: true),
                    FamilyName = table.Column<string>(type: "TEXT", nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Nationality = table.Column<string>(type: "TEXT", nullable: true),
                    PassportNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Gwf = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationPeople", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationPeople_VisaApplications_VisaApplicationId",
                        column: x => x.VisaApplicationId,
                        principalTable: "VisaApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimelineEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    VisaApplicationId = table.Column<int>(type: "INTEGER", nullable: true),
                    IngestRunId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimelineEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimelineEvents_IngestRuns_IngestRunId",
                        column: x => x.IngestRunId,
                        principalTable: "IngestRuns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TimelineEvents_VisaApplications_VisaApplicationId",
                        column: x => x.VisaApplicationId,
                        principalTable: "VisaApplications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationAnswer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationPersonId = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Answer = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationAnswer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationAnswer_ApplicationPeople_ApplicationPersonId",
                        column: x => x.ApplicationPersonId,
                        principalTable: "ApplicationPeople",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationAnswer_ApplicationPersonId",
                table: "ApplicationAnswer",
                column: "ApplicationPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationPeople_Gwf",
                table: "ApplicationPeople",
                column: "Gwf");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationPeople_VisaApplicationId",
                table: "ApplicationPeople",
                column: "VisaApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_IngestRuns_FileName",
                table: "IngestRuns",
                column: "FileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkippedRow_IngestRunId",
                table: "SkippedRow",
                column: "IngestRunId");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_IngestRunId",
                table: "TimelineEvents",
                column: "IngestRunId");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_VisaApplicationId",
                table: "TimelineEvents",
                column: "VisaApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplications_Gwf",
                table: "VisaApplications",
                column: "Gwf");

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplications_IngestRunId",
                table: "VisaApplications",
                column: "IngestRunId");

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplications_SubmissionGuid",
                table: "VisaApplications",
                column: "SubmissionGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplications_Uan",
                table: "VisaApplications",
                column: "Uan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationAnswer");

            migrationBuilder.DropTable(
                name: "SkippedRow");

            migrationBuilder.DropTable(
                name: "TimelineEvents");

            migrationBuilder.DropTable(
                name: "ApplicationPeople");

            migrationBuilder.DropTable(
                name: "VisaApplications");

            migrationBuilder.DropTable(
                name: "IngestRuns");
        }
    }
}
