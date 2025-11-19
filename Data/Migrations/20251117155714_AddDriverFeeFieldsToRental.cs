using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace carrentalmvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverFeeFieldsToRental : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualDriverFee",
                table: "Rentals",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedDriverFee",
                table: "Rentals",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualDriverFee",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "EstimatedDriverFee",
                table: "Rentals");
        }
    }
}
