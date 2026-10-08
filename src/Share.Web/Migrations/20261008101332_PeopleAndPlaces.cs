using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Share.Web.Migrations
{
    /// <inheritdoc />
    public partial class PeopleAndPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationAnswer_ApplicationPeople_ApplicationPersonId",
                table: "ApplicationAnswer");

            migrationBuilder.DropTable(
                name: "ApplicationPeople");

            migrationBuilder.RenameColumn(
                name: "ApplicationPersonId",
                table: "ApplicationAnswer",
                newName: "GuestId");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationAnswer_ApplicationPersonId",
                table: "ApplicationAnswer",
                newName: "IX_ApplicationAnswer_GuestId");

            migrationBuilder.AddColumn<int>(
                name: "AccommodationId",
                table: "VisaApplications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostFamilyName",
                table: "VisaApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HostGivenName",
                table: "VisaApplications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HostId",
                table: "VisaApplications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SponsorId",
                table: "VisaApplications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StayingWithSponsor",
                table: "VisaApplications",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccommodationsAdded",
                table: "IngestRuns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PeopleAdded",
                table: "IngestRuns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Accommodations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchKey = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    Postcode = table.Column<string>(type: "TEXT", nullable: true),
                    Council = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accommodations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Guests",
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
                    table.PrimaryKey("PK_Guests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guests_VisaApplications_VisaApplicationId",
                        column: x => x.VisaApplicationId,
                        principalTable: "VisaApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchKey = table.Column<string>(type: "TEXT", nullable: false),
                    GivenName = table.Column<string>(type: "TEXT", nullable: true),
                    FamilyName = table.Column<string>(type: "TEXT", nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Telephone = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Postcode = table.Column<string>(type: "TEXT", nullable: true),
                    Council = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplications_AccommodationId",
                table: "VisaApplications",
                column: "AccommodationId");

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplications_HostId",
                table: "VisaApplications",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_VisaApplications_SponsorId",
                table: "VisaApplications",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_MatchKey",
                table: "Accommodations",
                column: "MatchKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Guests_Gwf",
                table: "Guests",
                column: "Gwf");

            migrationBuilder.CreateIndex(
                name: "IX_Guests_VisaApplicationId",
                table: "Guests",
                column: "VisaApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_People_MatchKey",
                table: "People",
                column: "MatchKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationAnswer_Guests_GuestId",
                table: "ApplicationAnswer",
                column: "GuestId",
                principalTable: "Guests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VisaApplications_Accommodations_AccommodationId",
                table: "VisaApplications",
                column: "AccommodationId",
                principalTable: "Accommodations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VisaApplications_People_HostId",
                table: "VisaApplications",
                column: "HostId",
                principalTable: "People",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VisaApplications_People_SponsorId",
                table: "VisaApplications",
                column: "SponsorId",
                principalTable: "People",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationAnswer_Guests_GuestId",
                table: "ApplicationAnswer");

            migrationBuilder.DropForeignKey(
                name: "FK_VisaApplications_Accommodations_AccommodationId",
                table: "VisaApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_VisaApplications_People_HostId",
                table: "VisaApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_VisaApplications_People_SponsorId",
                table: "VisaApplications");

            migrationBuilder.DropTable(
                name: "Accommodations");

            migrationBuilder.DropTable(
                name: "Guests");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropIndex(
                name: "IX_VisaApplications_AccommodationId",
                table: "VisaApplications");

            migrationBuilder.DropIndex(
                name: "IX_VisaApplications_HostId",
                table: "VisaApplications");

            migrationBuilder.DropIndex(
                name: "IX_VisaApplications_SponsorId",
                table: "VisaApplications");

            migrationBuilder.DropColumn(
                name: "AccommodationId",
                table: "VisaApplications");

            migrationBuilder.DropColumn(
                name: "HostFamilyName",
                table: "VisaApplications");

            migrationBuilder.DropColumn(
                name: "HostGivenName",
                table: "VisaApplications");

            migrationBuilder.DropColumn(
                name: "HostId",
                table: "VisaApplications");

            migrationBuilder.DropColumn(
                name: "SponsorId",
                table: "VisaApplications");

            migrationBuilder.DropColumn(
                name: "StayingWithSponsor",
                table: "VisaApplications");

            migrationBuilder.DropColumn(
                name: "AccommodationsAdded",
                table: "IngestRuns");

            migrationBuilder.DropColumn(
                name: "PeopleAdded",
                table: "IngestRuns");

            migrationBuilder.RenameColumn(
                name: "GuestId",
                table: "ApplicationAnswer",
                newName: "ApplicationPersonId");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationAnswer_GuestId",
                table: "ApplicationAnswer",
                newName: "IX_ApplicationAnswer_ApplicationPersonId");

            migrationBuilder.CreateTable(
                name: "ApplicationPeople",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VisaApplicationId = table.Column<int>(type: "INTEGER", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    FamilyName = table.Column<string>(type: "TEXT", nullable: true),
                    GivenName = table.Column<string>(type: "TEXT", nullable: true),
                    Gwf = table.Column<string>(type: "TEXT", nullable: true),
                    Nationality = table.Column<string>(type: "TEXT", nullable: true),
                    PassportNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationPeople_Gwf",
                table: "ApplicationPeople",
                column: "Gwf");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationPeople_VisaApplicationId",
                table: "ApplicationPeople",
                column: "VisaApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationAnswer_ApplicationPeople_ApplicationPersonId",
                table: "ApplicationAnswer",
                column: "ApplicationPersonId",
                principalTable: "ApplicationPeople",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
