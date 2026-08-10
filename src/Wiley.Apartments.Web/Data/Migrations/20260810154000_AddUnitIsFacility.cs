using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wiley.Apartments.Web.Data.Migrations;

/// <inheritdoc />
public partial class AddUnitIsFacility : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsFacility",
            table: "Units",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "IX_Units_IsFacility",
            table: "Units",
            column: "IsFacility");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Units_IsFacility",
            table: "Units");

        migrationBuilder.DropColumn(
            name: "IsFacility",
            table: "Units");
    }
}
