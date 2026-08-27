using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wiley.Apartments.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilityReservationInventoryHeld : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InventoryHeld",
                table: "FacilityReservations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InventoryHeld",
                table: "FacilityReservations");
        }
    }
}
