using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddClientSignatureToContract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientSignature",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClientSignedAtUtc",
                table: "StudentContracts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContractSigningTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContractSigningTokens");

            migrationBuilder.DropColumn(
                name: "ClientSignature",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "ClientSignedAtUtc",
                table: "StudentContracts");
        }
    }
}
