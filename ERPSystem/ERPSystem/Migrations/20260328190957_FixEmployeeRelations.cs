using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixEmployeeRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeContact_EmployeeId",
                table: "EmployeeContact");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContact_EmployeeId",
                table: "EmployeeContact",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBank_EmployeeId",
                table: "EmployeeBank",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAddress_EmployeeId",
                table: "EmployeeAddress",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAddress_Employees_EmployeeId",
                table: "EmployeeAddress",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeBank_Employees_EmployeeId",
                table: "EmployeeBank",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAddress_Employees_EmployeeId",
                table: "EmployeeAddress");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeBank_Employees_EmployeeId",
                table: "EmployeeBank");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeContact_EmployeeId",
                table: "EmployeeContact");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeBank_EmployeeId",
                table: "EmployeeBank");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeAddress_EmployeeId",
                table: "EmployeeAddress");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContact_EmployeeId",
                table: "EmployeeContact",
                column: "EmployeeId");
        }
    }
}
