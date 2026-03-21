using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddActAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractAdditionalAct_ContractAdditionalAct_ContractAdditionalActId",
                table: "ContractAdditionalAct");

            migrationBuilder.DropIndex(
                name: "IX_ContractAdditionalAct_ContractAdditionalActId",
                table: "ContractAdditionalAct");

            migrationBuilder.DropColumn(
                name: "ContractAdditionalActId",
                table: "ContractAdditionalAct");

            migrationBuilder.RenameColumn(
                name: "IsSigned",
                table: "ContractAdditionalAct",
                newName: "IsSignedByStudent");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "ContractAdditionalAct",
                type: "int",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompanySignedAtUtc",
                table: "ContractAdditionalAct",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSignedByCompany",
                table: "ContractAdditionalAct",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StudentSignedAtUtc",
                table: "ContractAdditionalAct",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContractAdditionalActItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActId = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    CourseSessionId = table.Column<int>(type: "int", nullable: true),
                    StudentId = table.Column<int>(type: "int", nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractAdditionalActItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractAdditionalActItem_ContractAdditionalAct_ActId",
                        column: x => x.ActId,
                        principalTable: "ContractAdditionalAct",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractAdditionalActItem_ActId",
                table: "ContractAdditionalActItem",
                column: "ActId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractAdditionalActItem");

            migrationBuilder.DropColumn(
                name: "CompanySignedAtUtc",
                table: "ContractAdditionalAct");

            migrationBuilder.DropColumn(
                name: "IsSignedByCompany",
                table: "ContractAdditionalAct");

            migrationBuilder.DropColumn(
                name: "StudentSignedAtUtc",
                table: "ContractAdditionalAct");

            migrationBuilder.RenameColumn(
                name: "IsSignedByStudent",
                table: "ContractAdditionalAct",
                newName: "IsSigned");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ContractAdditionalAct",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "ContractAdditionalActId",
                table: "ContractAdditionalAct",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractAdditionalAct_ContractAdditionalActId",
                table: "ContractAdditionalAct",
                column: "ContractAdditionalActId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractAdditionalAct_ContractAdditionalAct_ContractAdditionalActId",
                table: "ContractAdditionalAct",
                column: "ContractAdditionalActId",
                principalTable: "ContractAdditionalAct",
                principalColumn: "Id");
        }
    }
}
