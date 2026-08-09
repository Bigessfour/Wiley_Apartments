using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wiley.Apartments.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaseCustomClauses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomClauses",
                table: "Leases",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomClauses",
                table: "Leases");
        }
    }
}
