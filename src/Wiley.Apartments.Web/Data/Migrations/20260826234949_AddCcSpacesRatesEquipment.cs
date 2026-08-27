using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wiley.Apartments.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCcSpacesRatesEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Space",
                table: "FacilityReservations",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "WholeBuilding");

            migrationBuilder.CreateTable(
                name: "FacilityRentalRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Space = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Fee = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Deposit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityRentalRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacilityReservationEquipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FacilityReservationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityReservationEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacilityReservationEquipment_FacilityInventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "FacilityInventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacilityReservationEquipment_FacilityReservations_FacilityReservationId",
                        column: x => x.FacilityReservationId,
                        principalTable: "FacilityReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityReservations_Space_Status",
                table: "FacilityReservations",
                columns: new[] { "Space", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityRentalRates_Space_IsActive_SortOrder",
                table: "FacilityRentalRates",
                columns: new[] { "Space", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityReservationEquipment_FacilityReservationId_InventoryItemId",
                table: "FacilityReservationEquipment",
                columns: new[] { "FacilityReservationId", "InventoryItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacilityReservationEquipment_InventoryItemId",
                table: "FacilityReservationEquipment",
                column: "InventoryItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacilityRentalRates");

            migrationBuilder.DropTable(
                name: "FacilityReservationEquipment");

            migrationBuilder.DropIndex(
                name: "IX_FacilityReservations_Space_Status",
                table: "FacilityReservations");

            migrationBuilder.DropColumn(
                name: "Space",
                table: "FacilityReservations");
        }
    }
}
