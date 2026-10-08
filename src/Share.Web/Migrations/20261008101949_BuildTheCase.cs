using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Share.Web.Migrations
{
    /// <inheritdoc />
    public partial class BuildTheCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CaseId",
                table: "VisaApplications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CaseId",
                table: "TimelineEvents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CasesAdded",
                table: "IngestRuns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchKey = table.Column<string>(type: "TEXT", nullable: false),
                    SponsorId = table.Column<int>(type: "INTEGER", nullable: true),
                    AccommodationId = table.Column<int>(type: "INTEGER", nullable: true),
                    Council = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cases_Accommodations_AccommodationId",
                        column: x => x.AccommodationId,
                        principalTable: "Accommodations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Cases_People_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "People",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplications_CaseId",
                table: "VisaApplications",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_CaseId",
                table: "TimelineEvents",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_AccommodationId",
                table: "Cases",
                column: "AccommodationId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_MatchKey",
                table: "Cases",
                column: "MatchKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_SponsorId",
                table: "Cases",
                column: "SponsorId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimelineEvents_Cases_CaseId",
                table: "TimelineEvents",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VisaApplications_Cases_CaseId",
                table: "VisaApplications",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimelineEvents_Cases_CaseId",
                table: "TimelineEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_VisaApplications_Cases_CaseId",
                table: "VisaApplications");

            migrationBuilder.DropTable(
                name: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_VisaApplications_CaseId",
                table: "VisaApplications");

            migrationBuilder.DropIndex(
                name: "IX_TimelineEvents_CaseId",
                table: "TimelineEvents");

            migrationBuilder.DropColumn(
                name: "CaseId",
                table: "VisaApplications");

            migrationBuilder.DropColumn(
                name: "CaseId",
                table: "TimelineEvents");

            migrationBuilder.DropColumn(
                name: "CasesAdded",
                table: "IngestRuns");
        }
    }
}
