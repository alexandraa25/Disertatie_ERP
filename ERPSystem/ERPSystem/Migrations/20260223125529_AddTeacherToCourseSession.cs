using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherToCourseSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeacherUserId",
                table: "Courses");

            migrationBuilder.AddColumn<string>(
                name: "TeacherUserId",
                table: "CourseSessions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSessions_TeacherUserId",
                table: "CourseSessions",
                column: "TeacherUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseSessions_AspNetUsers_TeacherUserId",
                table: "CourseSessions",
                column: "TeacherUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseSessions_AspNetUsers_TeacherUserId",
                table: "CourseSessions");

            migrationBuilder.DropIndex(
                name: "IX_CourseSessions_TeacherUserId",
                table: "CourseSessions");

            migrationBuilder.DropColumn(
                name: "TeacherUserId",
                table: "CourseSessions");

            migrationBuilder.AddColumn<string>(
                name: "TeacherUserId",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
