using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallmentsAndBodyEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "StudentContracts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "PdfPath",
                table: "StudentContracts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsBodyCustomized",
                table: "StudentContracts",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ContractBody",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "ContractInstallments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractInstallments_StudentContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "StudentContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentContracts_CreatedAtUtc",
                table: "StudentContracts",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StudentContracts_StartDate",
                table: "StudentContracts",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_StudentContracts_Status",
                table: "StudentContracts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContractInstallments_ContractId",
                table: "ContractInstallments",
                column: "ContractId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractInstallments");

            migrationBuilder.DropIndex(
                name: "IX_StudentContracts_CreatedAtUtc",
                table: "StudentContracts");

            migrationBuilder.DropIndex(
                name: "IX_StudentContracts_StartDate",
                table: "StudentContracts");

            migrationBuilder.DropIndex(
                name: "IX_StudentContracts_Status",
                table: "StudentContracts");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "StudentContracts",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "PdfPath",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsBodyCustomized",
                table: "StudentContracts",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "ContractBody",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
