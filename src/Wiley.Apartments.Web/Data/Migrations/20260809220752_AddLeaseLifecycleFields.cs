using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wiley.Apartments.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseLifecycleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LifecycleNote",
                table: "Leases",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PriorLeaseId",
                table: "Leases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SuccessorLeaseId",
                table: "Leases",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leases_PriorLeaseId",
                table: "Leases",
                column: "PriorLeaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Leases_Status_EndUtc",
                table: "Leases",
                columns: new[] { "Status", "EndUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Leases_PriorLeaseId",
                table: "Leases");

            migrationBuilder.DropIndex(
                name: "IX_Leases_Status_EndUtc",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "LifecycleNote",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "PriorLeaseId",
                table: "Leases");

            migrationBuilder.DropColumn(
                name: "SuccessorLeaseId",
                table: "Leases");
        }
    }
}
