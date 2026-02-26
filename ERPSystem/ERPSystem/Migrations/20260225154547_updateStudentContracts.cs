using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Migrations
{
    /// <inheritdoc />
    public partial class updateStudentContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractCourses_Courses_CourseId",
                table: "ContractCourses");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentContracts_Students_StudentId",
                table: "StudentContracts");

            migrationBuilder.DropIndex(
                name: "IX_StudentContracts_StudentId",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ContractCourses");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "ContractCourses");

            migrationBuilder.RenameColumn(
                name: "IsSigned",
                table: "StudentContracts",
                newName: "IsUnlimited");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "ContractCourses",
                newName: "CourseSessionId");

            migrationBuilder.RenameIndex(
                name: "IX_ContractCourses_CourseId",
                table: "ContractCourses",
                newName: "IX_ContractCourses_CourseSessionId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "StudentContracts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "ContractBody",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizedAtUtc",
                table: "StudentContracts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfPath",
                table: "StudentContracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignedAtUtc",
                table: "StudentContracts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Fee",
                table: "CourseSessions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "CourseSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CourseNameSnapshot",
                table: "ContractCourses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PriceSnapshot",
                table: "ContractCourses",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SessionNameSnapshot",
                table: "ContractCourses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ContractDiscounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractDiscounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractDiscounts_StudentContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "StudentContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractParties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: true),
                    GuardianId = table.Column<int>(type: "int", nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractParties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractParties_Guardians_GuardianId",
                        column: x => x.GuardianId,
                        principalTable: "Guardians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractParties_StudentContracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "StudentContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractParties_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentContracts_ContractNumber",
                table: "StudentContracts",
                column: "ContractNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractDiscounts_ContractId",
                table: "ContractDiscounts",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractParties_ContractId",
                table: "ContractParties",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractParties_GuardianId",
                table: "ContractParties",
                column: "GuardianId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractParties_StudentId",
                table: "ContractParties",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractCourses_CourseSessions_CourseSessionId",
                table: "ContractCourses",
                column: "CourseSessionId",
                principalTable: "CourseSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContractCourses_CourseSessions_CourseSessionId",
                table: "ContractCourses");

            migrationBuilder.DropTable(
                name: "ContractDiscounts");

            migrationBuilder.DropTable(
                name: "ContractParties");

            migrationBuilder.DropIndex(
                name: "IX_StudentContracts_ContractNumber",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "ContractBody",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "FinalizedAtUtc",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "PdfPath",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "SignedAtUtc",
                table: "StudentContracts");

            migrationBuilder.DropColumn(
                name: "Fee",
                table: "CourseSessions");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "CourseSessions");

            migrationBuilder.DropColumn(
                name: "CourseNameSnapshot",
                table: "ContractCourses");

            migrationBuilder.DropColumn(
                name: "PriceSnapshot",
                table: "ContractCourses");

            migrationBuilder.DropColumn(
                name: "SessionNameSnapshot",
                table: "ContractCourses");

            migrationBuilder.RenameColumn(
                name: "IsUnlimited",
                table: "StudentContracts",
                newName: "IsSigned");

            migrationBuilder.RenameColumn(
                name: "CourseSessionId",
                table: "ContractCourses",
                newName: "CourseId");

            migrationBuilder.RenameIndex(
                name: "IX_ContractCourses_CourseSessionId",
                table: "ContractCourses",
                newName: "IX_ContractCourses_CourseId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "StudentContracts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentId",
                table: "StudentContracts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "ContractCourses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "ContractCourses",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_StudentContracts_StudentId",
                table: "StudentContracts",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContractCourses_Courses_CourseId",
                table: "ContractCourses",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentContracts_Students_StudentId",
                table: "StudentContracts",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
