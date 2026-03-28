using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateActAdd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractSigningTokens");

            migrationBuilder.DropColumn(
                name: "IsSignedByCompany",
                table: "ContractAdditionalAct");

            migrationBuilder.DropColumn(
                name: "IsSignedByStudent",
                table: "ContractAdditionalAct");

            migrationBuilder.RenameColumn(
                name: "StudentSignedAtUtc",
                table: "ContractAdditionalAct",
                newName: "ClientSignedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CompanySignedAtUtc",
                table: "ContractAdditionalAct",
                newName: "AdminSignedAtUtc");

            migrationBuilder.AddColumn<string>(
                name: "AdminSignature",
                table: "ContractAdditionalAct",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientSignature",
                table: "ContractAdditionalAct",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfPath",
                table: "ContractAdditionalAct",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SigningTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SigningTokens", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SigningTokens");

            migrationBuilder.DropColumn(
                name: "AdminSignature",
                table: "ContractAdditionalAct");

            migrationBuilder.DropColumn(
                name: "ClientSignature",
                table: "ContractAdditionalAct");

            migrationBuilder.DropColumn(
                name: "PdfPath",
                table: "ContractAdditionalAct");

            migrationBuilder.RenameColumn(
                name: "ClientSignedAtUtc",
                table: "ContractAdditionalAct",
                newName: "StudentSignedAtUtc");

            migrationBuilder.RenameColumn(
                name: "AdminSignedAtUtc",
                table: "ContractAdditionalAct",
                newName: "CompanySignedAtUtc");

            migrationBuilder.AddColumn<bool>(
                name: "IsSignedByCompany",
                table: "ContractAdditionalAct",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSignedByStudent",
                table: "ContractAdditionalAct",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ContractSigningTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractSigningTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractSigningTokens_StudentContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "StudentContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContractSigningTokens_ContractId",
                table: "ContractSigningTokens",
                column: "ContractId");
        }
    }
}
