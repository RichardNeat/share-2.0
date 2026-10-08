using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Share.Web.Migrations
{
    /// <inheritdoc />
    public partial class FreshOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OfferId",
                table: "TimelineEvents",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HostId",
                table: "Cases",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RematchedAt",
                table: "Cases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubmissionReference = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HostName = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Telephone = table.Column<string>(type: "TEXT", nullable: true),
                    Address1 = table.Column<string>(type: "TEXT", nullable: true),
                    Address2 = table.Column<string>(type: "TEXT", nullable: true),
                    TownOrCity = table.Column<string>(type: "TEXT", nullable: true),
                    Postcode = table.Column<string>(type: "TEXT", nullable: true),
                    Council = table.Column<string>(type: "TEXT", nullable: true),
                    AvailableFrom = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Adults = table.Column<int>(type: "INTEGER", nullable: true),
                    Children = table.Column<int>(type: "INTEGER", nullable: true),
                    Bedrooms = table.Column<int>(type: "INTEGER", nullable: true),
                    StepFree = table.Column<bool>(type: "INTEGER", nullable: true),
                    Pets = table.Column<bool>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    TakenByCaseId = table.Column<int>(type: "INTEGER", nullable: true),
                    TakenAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_Cases_TakenByCaseId",
                        column: x => x.TakenByCaseId,
                        principalTable: "Cases",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_OfferId",
                table: "TimelineEvents",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_HostId",
                table: "Cases",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_SubmissionReference",
                table: "Offers",
                column: "SubmissionReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_TakenByCaseId",
                table: "Offers",
                column: "TakenByCaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_People_HostId",
                table: "Cases",
                column: "HostId",
                principalTable: "People",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TimelineEvents_Offers_OfferId",
                table: "TimelineEvents",
                column: "OfferId",
                principalTable: "Offers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_People_HostId",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_TimelineEvents_Offers_OfferId",
                table: "TimelineEvents");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropIndex(
                name: "IX_TimelineEvents_OfferId",
                table: "TimelineEvents");

            migrationBuilder.DropIndex(
                name: "IX_Cases_HostId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "OfferId",
                table: "TimelineEvents");

            migrationBuilder.DropColumn(
                name: "HostId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "RematchedAt",
                table: "Cases");
        }
    }
}
