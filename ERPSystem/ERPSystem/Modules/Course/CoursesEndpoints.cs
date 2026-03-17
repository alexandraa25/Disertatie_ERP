using ERPSystem.Extensions;
using ERPSystem.Modules.Course.Models;
using ERPSystem.Shared.BusinessLogic;
using Route = ERPSystem.Utils.Constants.General.Route.Courses;

namespace ERPSystem.Modules.Courses;

public static class CoursesEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet(Route.COURSES,
            async (string? q, CoursesService service)
                => await service.ListAsync(q))
            .WithDefaultApiSettings("GetAllCourses", "Lista cursuri", "GET", false);

        group.MapGet(Route.COURSE_BY_ID,
            async (int id, CoursesService service)
                => await service.GetAsync(id))
            .WithDefaultApiSettings("GetCourseById", "Detalii curs", "GET_BY_ID", false);

        group.MapPost(Route.COURSES,
            async (CreateCourseDto dto, CoursesService service)
                => await service.CreateAsync(dto))
            .WithDefaultApiSettings("CreateCourse", "Creare curs", "CREATE", false);

        group.MapPut(Route.COURSE_BY_ID,
            async (int id, UpdateCourseDto dto, CoursesService service)
                => await service.UpdateAsync(id, dto))
            .WithDefaultApiSettings("UpdateCourse", "Actualizare curs", "UPDATE", false);

        group.MapDelete(Route.COURSE_BY_ID,
            async (int id, CoursesService service)
                => await service.DeleteAsync(id))
            .WithDefaultApiSettings("DeleteCourse", "Stergere curs", "DELETE", false);

        group.MapGet(Route.COURSE_ENROLLMENTS,
            async (int id, CoursesService service)
                => await service.ListEnrollmentsAsync(id))
            .WithDefaultApiSettings("GetCourseEnrollments", "Lista inscrieri curs", "ENROLLMENTS_LIST", false);

        group.MapPost(Route.COURSE_ENROLLMENTS,
            async (int id, EnrollStudentRequest body, CoursesService service)
                => await service.EnrollStudentAsync(id, body.StudentId, body.SessionId));

        group.MapPut(Route.COURSE_ENROLLMENT_BY_SESSION_STUDENT,
            async (int id, int sessionId, int studentId, bool isActive, CoursesService service)
                => await service.SetEnrollmentActiveAsync(id, sessionId, studentId, isActive));

        group.MapGet(Route.COURSE_TEACHERS,
            async (CoursesService service)
                => await service.GetTeachersAsync())
            .WithDefaultApiSettings("GetTeachers", "Lista profesori", "TEACHERS", false);

        group.MapGet(Route.COURSE_AVAILABLE_STUDENTS,
           async (int id, int sessionId, string? q, CoursesService service)
               => await service.GetAvailableStudentsAsync(id, sessionId, q))
         .WithDefaultApiSettings("GetAvailableStudents","Lista cursanti disponibili pentru sesiune", "AVAILABLE_STUDENTS_LIST", false );
    }
}
