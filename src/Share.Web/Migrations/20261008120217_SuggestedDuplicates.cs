using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Share.Web.Migrations
{
    /// <inheritdoc />
    public partial class SuggestedDuplicates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DuplicateOfGuestId",
                table: "Guests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MarkedDuplicateAt",
                table: "Guests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DuplicateDismissals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PairKey = table.Column<string>(type: "TEXT", nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuplicateDismissals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guests_DuplicateOfGuestId",
                table: "Guests",
                column: "DuplicateOfGuestId");

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateDismissals_PairKey",
                table: "DuplicateDismissals",
                column: "PairKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Guests_Guests_DuplicateOfGuestId",
                table: "Guests",
                column: "DuplicateOfGuestId",
                principalTable: "Guests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guests_Guests_DuplicateOfGuestId",
                table: "Guests");

            migrationBuilder.DropTable(
                name: "DuplicateDismissals");

            migrationBuilder.DropIndex(
                name: "IX_Guests_DuplicateOfGuestId",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "DuplicateOfGuestId",
                table: "Guests");

            migrationBuilder.DropColumn(
                name: "MarkedDuplicateAt",
                table: "Guests");
        }
    }
}
