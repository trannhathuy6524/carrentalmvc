using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace carrentalmvc.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureModel3DTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarModel3Ds_Cars_CarId",
                table: "CarModel3Ds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CarModel3Ds",
                table: "CarModel3Ds");

            migrationBuilder.RenameTable(
                name: "CarModel3Ds",
                newName: "CarModel3D");

            migrationBuilder.RenameIndex(
                name: "IX_CarModel3Ds_CarId",
                table: "CarModel3D",
                newName: "IX_CarModel3D_CarId");

            migrationBuilder.AddColumn<int>(
                name: "TemplateId",
                table: "CarModel3D",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CarModel3D",
                table: "CarModel3D",
                column: "CarModel3DId");

            migrationBuilder.CreateTable(
                name: "Model3DTemplates",
                columns: table => new
                {
                    TemplateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PreviewImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileFormat = table.Column<int>(type: "int", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    BrandId = table.Column<int>(type: "int", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Model3DTemplates", x => x.TemplateId);
                    table.ForeignKey(
                        name: "FK_Model3DTemplates_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "BrandId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Model3DTemplates_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarModel3D_TemplateId",
                table: "CarModel3D",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Model3DTemplates_BrandId",
                table: "Model3DTemplates",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Model3DTemplates_CategoryId",
                table: "Model3DTemplates",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_CarModel3D_Cars_CarId",
                table: "CarModel3D",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "CarId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CarModel3D_Model3DTemplates_TemplateId",
                table: "CarModel3D",
                column: "TemplateId",
                principalTable: "Model3DTemplates",
                principalColumn: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarModel3D_Cars_CarId",
                table: "CarModel3D");

            migrationBuilder.DropForeignKey(
                name: "FK_CarModel3D_Model3DTemplates_TemplateId",
                table: "CarModel3D");

            migrationBuilder.DropTable(
                name: "Model3DTemplates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CarModel3D",
                table: "CarModel3D");

            migrationBuilder.DropIndex(
                name: "IX_CarModel3D_TemplateId",
                table: "CarModel3D");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "CarModel3D");

            migrationBuilder.RenameTable(
                name: "CarModel3D",
                newName: "CarModel3Ds");

            migrationBuilder.RenameIndex(
                name: "IX_CarModel3D_CarId",
                table: "CarModel3Ds",
                newName: "IX_CarModel3Ds_CarId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CarModel3Ds",
                table: "CarModel3Ds",
                column: "CarModel3DId");

            migrationBuilder.AddForeignKey(
                name: "FK_CarModel3Ds_Cars_CarId",
                table: "CarModel3Ds",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "CarId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
