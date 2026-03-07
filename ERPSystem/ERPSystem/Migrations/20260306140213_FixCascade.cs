using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_CourseSessions_CourseSessionId",
                table: "CourseEnrollments");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_CourseSessions_CourseSessionId",
                table: "CourseEnrollments",
                column: "CourseSessionId",
                principalTable: "CourseSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_CourseSessions_CourseSessionId",
                table: "CourseEnrollments");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_CourseSessions_CourseSessionId",
                table: "CourseEnrollments",
                column: "CourseSessionId",
                principalTable: "CourseSessions",
                principalColumn: "Id");
        }
    }
}
