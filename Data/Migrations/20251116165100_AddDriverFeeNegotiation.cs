using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace carrentalmvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverFeeNegotiation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AgreedDailyFee",
                table: "DriverRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CounterOfferDailyFee",
                table: "DriverRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedDailyFee",
                table: "DriverRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyDriverFee",
                table: "DriverAssignments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgreedDailyFee",
                table: "DriverRequests");

            migrationBuilder.DropColumn(
                name: "CounterOfferDailyFee",
                table: "DriverRequests");

            migrationBuilder.DropColumn(
                name: "RequestedDailyFee",
                table: "DriverRequests");

            migrationBuilder.DropColumn(
                name: "DailyDriverFee",
                table: "DriverAssignments");
        }
    }
}
