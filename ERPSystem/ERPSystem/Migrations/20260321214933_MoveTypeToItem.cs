using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Migrations
{
    /// <inheritdoc />
    public partial class MoveTypeToItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceDifference",
                table: "ContractAdditionalAct");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ContractAdditionalAct");

            migrationBuilder.RenameColumn(
                name: "ChangeType",
                table: "ContractAdditionalActItem",
                newName: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "ContractAdditionalActItem",
                newName: "ChangeType");

            migrationBuilder.AddColumn<decimal>(
                name: "PriceDifference",
                table: "ContractAdditionalAct",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "ContractAdditionalAct",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
