using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace carrentalmvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverRequestSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DriverAccepted",
                table: "Rentals",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DriverAssignedAt",
                table: "Rentals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverId",
                table: "Rentals",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresDriver",
                table: "Rentals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DriverAssignments",
                columns: table => new
                {
                    DriverAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarOwnerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DriverId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverAssignments", x => x.DriverAssignmentId);
                    table.ForeignKey(
                        name: "FK_DriverAssignments_AspNetUsers_CarOwnerId",
                        column: x => x.CarOwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverAssignments_AspNetUsers_DriverId",
                        column: x => x.DriverId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DriverRequests",
                columns: table => new
                {
                    DriverRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CarOwnerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DriverLicense = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Experience = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverRequests", x => x.DriverRequestId);
                    table.ForeignKey(
                        name: "FK_DriverRequests_AspNetUsers_CarOwnerId",
                        column: x => x.CarOwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DriverRequests_AspNetUsers_ProcessedBy",
                        column: x => x.ProcessedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DriverRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rentals_DriverId",
                table: "Rentals",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverAssignments_CarOwnerId",
                table: "DriverAssignments",
                column: "CarOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverAssignments_DriverId",
                table: "DriverAssignments",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverRequests_CarOwnerId",
                table: "DriverRequests",
                column: "CarOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverRequests_ProcessedBy",
                table: "DriverRequests",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DriverRequests_Status",
                table: "DriverRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DriverRequests_UserId",
                table: "DriverRequests",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rentals_AspNetUsers_DriverId",
                table: "Rentals",
                column: "DriverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rentals_AspNetUsers_DriverId",
                table: "Rentals");

            migrationBuilder.DropTable(
                name: "DriverAssignments");

            migrationBuilder.DropTable(
                name: "DriverRequests");

            migrationBuilder.DropIndex(
                name: "IX_Rentals_DriverId",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "DriverAccepted",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "DriverAssignedAt",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "DriverId",
                table: "Rentals");

            migrationBuilder.DropColumn(
                name: "RequiresDriver",
                table: "Rentals");
        }
    }
}
