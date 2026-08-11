using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wiley.Apartments.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityCenterFacility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FacilityReservationId",
                table: "ScheduledItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletedByDisplay",
                table: "MaintenanceRequests",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletedByUserId",
                table: "MaintenanceRequests",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FacilityReservationId",
                table: "MaintenanceRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "LedgerEntries",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "FacilityRenterId",
                table: "LedgerEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FacilityReservationId",
                table: "LedgerEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FacilityInventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Condition = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Serial = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    RowVersion = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityInventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacilityInventoryItems_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FacilityRenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Organization = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    MailingAddress = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AlternateContact = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    IdType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IdReference = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    RowVersion = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityRenters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacilityReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FacilityRenterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    RentalFee = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    GeneratedPdfRelativePath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    SignedDocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ScheduledItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    RowVersion = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacilityReservations_FacilityRenters_FacilityRenterId",
                        column: x => x.FacilityRenterId,
                        principalTable: "FacilityRenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacilityReservations_Units_UnitId",
                        column: x => x.UnitId,
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FacilityInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FacilityReservationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsSatisfactory = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChecklistNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    DamageNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    InspectorUserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    InspectorDisplay = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    InspectedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityInspections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacilityInspections_FacilityReservations_FacilityReservationId",
                        column: x => x.FacilityReservationId,
                        principalTable: "FacilityReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledItems_FacilityReservationId",
                table: "ScheduledItems",
                column: "FacilityReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_FacilityReservationId",
                table: "MaintenanceRequests",
                column: "FacilityReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_FacilityRenterId",
                table: "LedgerEntries",
                column: "FacilityRenterId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_FacilityReservationId",
                table: "LedgerEntries",
                column: "FacilityReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityInspections_FacilityReservationId_Type",
                table: "FacilityInspections",
                columns: new[] { "FacilityReservationId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityInventoryItems_UnitId_Category_IsDeleted",
                table: "FacilityInventoryItems",
                columns: new[] { "UnitId", "Category", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityRenters_LastName_IsDeleted",
                table: "FacilityRenters",
                columns: new[] { "LastName", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityReservations_FacilityRenterId_IsDeleted",
                table: "FacilityReservations",
                columns: new[] { "FacilityRenterId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityReservations_StartUtc_EndUtc",
                table: "FacilityReservations",
                columns: new[] { "StartUtc", "EndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityReservations_UnitId_IsDeleted_Status",
                table: "FacilityReservations",
                columns: new[] { "UnitId", "IsDeleted", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_LedgerEntries_FacilityRenters_FacilityRenterId",
                table: "LedgerEntries",
                column: "FacilityRenterId",
                principalTable: "FacilityRenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LedgerEntries_FacilityReservations_FacilityReservationId",
                table: "LedgerEntries",
                column: "FacilityReservationId",
                principalTable: "FacilityReservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceRequests_FacilityReservations_FacilityReservationId",
                table: "MaintenanceRequests",
                column: "FacilityReservationId",
                principalTable: "FacilityReservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledItems_FacilityReservations_FacilityReservationId",
                table: "ScheduledItems",
                column: "FacilityReservationId",
                principalTable: "FacilityReservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LedgerEntries_FacilityRenters_FacilityRenterId",
                table: "LedgerEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_LedgerEntries_FacilityReservations_FacilityReservationId",
                table: "LedgerEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceRequests_FacilityReservations_FacilityReservationId",
                table: "MaintenanceRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledItems_FacilityReservations_FacilityReservationId",
                table: "ScheduledItems");

            migrationBuilder.DropTable(
                name: "FacilityInspections");

            migrationBuilder.DropTable(
                name: "FacilityInventoryItems");

            migrationBuilder.DropTable(
                name: "FacilityReservations");

            migrationBuilder.DropTable(
                name: "FacilityRenters");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledItems_FacilityReservationId",
                table: "ScheduledItems");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRequests_FacilityReservationId",
                table: "MaintenanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_FacilityRenterId",
                table: "LedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_FacilityReservationId",
                table: "LedgerEntries");

            migrationBuilder.DropColumn(
                name: "FacilityReservationId",
                table: "ScheduledItems");

            migrationBuilder.DropColumn(
                name: "CompletedByDisplay",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "CompletedByUserId",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "FacilityReservationId",
                table: "MaintenanceRequests");

            migrationBuilder.DropColumn(
                name: "FacilityRenterId",
                table: "LedgerEntries");

            migrationBuilder.DropColumn(
                name: "FacilityReservationId",
                table: "LedgerEntries");

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "LedgerEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
