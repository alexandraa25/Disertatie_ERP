using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalActs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ContractId",
                table: "CourseEnrollments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAtUtc",
                table: "CourseEnrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContractAdditionalAct",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    ActNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PriceDifference = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSigned = table.Column<bool>(type: "bit", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ContractAdditionalActId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractAdditionalAct", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractAdditionalAct_ContractAdditionalAct_ContractAdditionalActId",
                        column: x => x.ContractAdditionalActId,
                        principalTable: "ContractAdditionalAct",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContractAdditionalAct_StudentContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "StudentContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_ContractId",
                table: "CourseEnrollments",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAdditionalAct_ContractAdditionalActId",
                table: "ContractAdditionalAct",
                column: "ContractAdditionalActId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAdditionalAct_ContractId",
                table: "ContractAdditionalAct",
                column: "ContractId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_StudentContracts_ContractId",
                table: "CourseEnrollments",
                column: "ContractId",
                principalTable: "StudentContracts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_StudentContracts_ContractId",
                table: "CourseEnrollments");

            migrationBuilder.DropTable(
                name: "ContractAdditionalAct");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_ContractId",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "ContractId",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "EndedAtUtc",
                table: "CourseEnrollments");
        }
    }
}
