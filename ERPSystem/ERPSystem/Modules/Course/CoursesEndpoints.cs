using ERPSystem.Data.Entities;
using ERPSystem.Extensions;
using ERPSystem.Modules.Course.Models;
using ERPSystem.Shared.BusinessLogic;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Route = ERPSystem.Utils.Constants.General.Route.Courses;

namespace ERPSystem.Modules.Courses;

public static class CoursesEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(Route.COURSES,
            async (string? q, string? status, string? deleteStatus, DiscountScope? scope, CoursesService service)
                 => await service.ListAsync(q, status, deleteStatus,  scope))
            .RequireAuthorization(policy =>policy.RequireRole("Admin", "Manager", "Secretary", "Teacher"))
            .WithDefaultApiSettings("GetAllCourses", "Lista cursuri", "GET", false);

        group.MapGet(Route.COURSE_BY_ID,
            async (int id, CoursesService service)
                => await service.GetAsync(id))
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "Secretary", "Teacher"))
            .WithDefaultApiSettings("GetCourseById", "Detalii curs", "GET_BY_ID", false);

        group.MapPost(Route.COURSES,
            async (CreateCourseDto dto, CoursesService service)
                => await service.CreateAsync(dto))
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "Secretary"))
            .WithDefaultApiSettings("CreateCourse", "Creare curs", "CREATE", false);

        group.MapPut(Route.COURSE_BY_ID,
            async (int id, UpdateCourseDto dto, CoursesService service)
                => await service.UpdateAsync(id, dto))
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "Secretary"))
            .WithDefaultApiSettings("UpdateCourse", "Actualizare curs", "UPDATE", false);

        group.MapPost(Route.COURSE_STATUS,
           async (int id, CoursesService service)
               => await service.ToggleCourseStatusAsync(id))
            .RequireAuthorization(policy =>policy.RequireRole("Admin", "Manager", "Secretary"))
           .WithDefaultApiSettings("ToggleCourseStatus", "Activare/Dezactivare curs", "UPDATE", false);

        group.MapDelete(Route.COURSE_DELETE,
            async (int id, CoursesService service)
                => await service.DeleteAsync(id))
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "Secretary"))
            .WithDefaultApiSettings("DeleteCourse", "Șterge curs", "DELETE", false);
       
        group.MapPost(Route.COURSE_RESTORE,
            async (int id, CoursesService service)
                => await service.RestoreAsync(id))
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "Secretary"))
            .WithDefaultApiSettings("RestoreCourse", "Restaurează curs", "RESTORE", false);

        group.MapPatch(Route.SESSION_STATUS,
             async (int id, CoursesService service)
                 => await service.ToggleSessionStatusAsync(id))
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "Secretary"))
             .WithDefaultApiSettings( "ToggleSessionStatus", "Activare/Dezactivare sesiune", "UPDATE",  false);

        group.MapGet(Route.COURSE_ENROLLMENTS,
            async (int courseId, int? sessionId, CoursesService service)
                => await service.ListEnrollmentsAsync(courseId, sessionId))
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "Secretary", "Teacher"))
            .WithDefaultApiSettings("GetCourseEnrollments", "Lista inscrieri curs", "ENROLLMENTS_LIST", false);

        group.MapPost(Route.COURSE_ENROLLMENTS,
           async (int courseId, EnrollStudentRequest body, CoursesService service)
                => await service.EnrollStudentAsync(courseId, body.StudentId, body.SessionId))
          .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "Secretary", "Teacher"));

        group.MapPut(Route.COURSE_ENROLLMENT_BY_SESSION_STUDENT,
            async (int id, int sessionId, int studentId, bool isActive, CoursesService service)
                => await service.SetEnrollmentActiveAsync(id, sessionId, studentId, isActive))
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "Secretary", "Teacher"));

        group.MapGet(Route.COURSE_TEACHERS,
            async (CoursesService service)
                => await service.GetTeachersAsync())
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "Secretary", "Teacher"))
            .WithDefaultApiSettings("GetTeachers", "Lista profesori", "TEACHERS", false);

        group.MapGet(Route.COURSE_AVAILABLE_STUDENTS,
            async (int id, int sessionId, string? q, CoursesService service)
               => await service.GetAvailableStudentsAsync(id, sessionId, q))
           .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "Secretary", "Teacher"))
           .WithDefaultApiSettings("GetAvailableStudents","Lista cursanti disponibili pentru sesiune", "AVAILABLE_STUDENTS_LIST", false );

        group.MapGet(Route.EXPORT_COURSES_EXCEL,
           async ( string? q,string? status,string? deleteStatus, DiscountScope? scope, CoursesService service)
                => await service.ExportCoursesExcelAsync(q, status, deleteStatus, scope))
           .RequireAuthorization(policy => policy.RequireRole("Admin", "Manager", "Secretary", "Teacher"))
          .WithDefaultApiSettings( "ExportCoursesExcel","Exportă cursurile și sesiunile în Excel","EXPORT_COURSES_EXCEL", true);
    }
}
