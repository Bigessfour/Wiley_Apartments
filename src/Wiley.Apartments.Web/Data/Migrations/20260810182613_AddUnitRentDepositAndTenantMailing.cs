using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wiley.Apartments.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitRentDepositAndTenantMailing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHandicapAccessible",
                table: "Units",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LeaseTerm",
                table: "Units",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyRent",
                table: "Units",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SecurityDeposit",
                table: "Units",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MailingAddress",
                table: "Tenants",
                type: "TEXT",
                maxLength: 512,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHandicapAccessible",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "LeaseTerm",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "MonthlyRent",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "SecurityDeposit",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "MailingAddress",
                table: "Tenants");
        }
    }
}
