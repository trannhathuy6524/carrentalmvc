using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace carrentalmvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryConfigToCar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxDeliveryDistance",
                table: "Cars",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerKmDelivery",
                table: "Cars",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxDeliveryDistance",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "PricePerKmDelivery",
                table: "Cars");
        }
    }
}
