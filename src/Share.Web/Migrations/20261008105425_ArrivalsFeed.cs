using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Share.Web.Migrations
{
    /// <inheritdoc />
    public partial class ArrivalsFeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Arrivals",
                table: "IngestRuns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StatusesChanged",
                table: "IngestRuns",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DecisionUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VisaApplicationId = table.Column<int>(type: "INTEGER", nullable: false),
                    IngestRunId = table.Column<int>(type: "INTEGER", nullable: false),
                    RowNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Gwf = table.Column<string>(type: "TEXT", nullable: true),
                    Uan = table.Column<string>(type: "TEXT", nullable: true),
                    DecisionDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Decision = table.Column<string>(type: "TEXT", nullable: true),
                    VoyageCode = table.Column<string>(type: "TEXT", nullable: true),
                    ArrivalPort = table.Column<string>(type: "TEXT", nullable: true),
                    ArrivedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PersonIdentifier = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecisionUpdates_IngestRuns_IngestRunId",
                        column: x => x.IngestRunId,
                        principalTable: "IngestRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecisionUpdates_VisaApplications_VisaApplicationId",
                        column: x => x.VisaApplicationId,
                        principalTable: "VisaApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DecisionUpdates_IngestRunId_RowNumber",
                table: "DecisionUpdates",
                columns: new[] { "IngestRunId", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DecisionUpdates_VisaApplicationId",
                table: "DecisionUpdates",
                column: "VisaApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DecisionUpdates");

            migrationBuilder.DropColumn(
                name: "Arrivals",
                table: "IngestRuns");

            migrationBuilder.DropColumn(
                name: "StatusesChanged",
                table: "IngestRuns");
        }
    }
}
