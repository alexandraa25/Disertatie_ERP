using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddContractSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SignedAtUtc",
                table: "StudentContracts",
                newName: "AdminSignedAtUtc");

            migrationBuilder.AlterColumn<string>(
                name: "PdfPath",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClientSignature",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminSignature",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryAddressSnapshot",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryEmailSnapshot",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryNameSnapshot",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryPhoneSnapshot",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryRoleSnapshot",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyAddressSnapshot",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyBankSnapshot",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyCuiSnapshot",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyEmailSnapshot",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyIbanSnapshot",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyNameSnapshot",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyPhoneSnapshot",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanyRegistrationSnapshot",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminSignature",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "BeneficiaryAddressSnapshot",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "BeneficiaryEmailSnapshot",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "BeneficiaryNameSnapshot",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "BeneficiaryPhoneSnapshot",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "BeneficiaryRoleSnapshot",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "CompanyAddressSnapshot",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "CompanyBankSnapshot",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "CompanyCuiSnapshot",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "CompanyEmailSnapshot",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "CompanyIbanSnapshot",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "CompanyNameSnapshot",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "CompanyPhoneSnapshot",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "CompanyRegistrationSnapshot",
                table: "StudentContracts");

            migrationBuilder.RenameColumn(
                name: "AdminSignedAtUtc",
                table: "StudentContracts",
                newName: "SignedAtUtc");

            migrationBuilder.AlterColumn<string>(
                name: "PdfPath",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ClientSignature",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
