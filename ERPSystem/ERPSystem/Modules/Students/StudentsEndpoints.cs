using ERPSystem.Extensions;
using ERPSystem.Modules.Student;
using ERPSystem.Modules.Student.Models;
using Route = ERPSystem.Utils.Constants.General.Route.Students;

namespace ERPSystem.Modules.Students
{
    public static class StudentsEndpoints
    {
        public static void Map(RouteGroupBuilder group)
        {
            group.MapGet(Route.STUDENTS,
                async (string? q, int page, int pageSize, string? sortBy, string? sortDir, int? recentDays, bool? onlyRecent,
                       StudentsService studentsService)
                    => await studentsService.GetStudentsAsync(q, page, pageSize, sortBy, sortDir, recentDays, onlyRecent))
                .WithDefaultApiSettings("GetStudents", "Lista elevi (paging/sort/filter)", "GET", false);

            group.MapGet(Route.STUDENT_BY_ID,
                async (int id, StudentsService studentsService)
                    => await studentsService.GetByIdAsync(id))
                .WithDefaultApiSettings("GetStudentById", "Detalii elev", "GET_BY_ID", false);

            group.MapPost(Route.STUDENTS,
                async (CreateStudentDto request, StudentsService studentsService)
                    => await studentsService.CreateAsync(request))
                .WithDefaultApiSettings("CreateStudent", "Creare elev", "CREATE", false);

            group.MapPut(Route.STUDENT_BY_ID,
                async (int id, UpdateStudentDto request, StudentsService studentsService)
                    => await studentsService.UpdateAsync(id, request))
                .WithDefaultApiSettings("UpdateStudent", "Actualizare elev", "UPDATE", false);

            group.MapDelete(Route.STUDENT_BY_ID,
                async (int id, StudentsService studentsService)
                    => await studentsService.DeleteAsync(id))
                .WithDefaultApiSettings("DeleteStudent", "Ștergere elev", "DELETE", false);

            group.MapGet(Route.STUDENT_OPTIONS,
                async (string? q, StudentsService studentsService)
                    => await studentsService.SearchOptionsAsync(q))
               .WithDefaultApiSettings("SearchStudentOptions", "Căutare elevi pentru dropdown (autocomplete)", "GET", false);

            group.MapGet(Route.STUDENT_COURSES,
                 async (int id, StudentsService studentsService)
                     => await studentsService.GetStudentCoursesAsync(id))
                .WithDefaultApiSettings("GetStudentCourses", "Lista cursuri si sesiuni asociate elevului", "GET_STUDENT_COURSES",   false  );
        
            group.MapGet(Route.GUARDIAN_OPTIONS,
                 async (int id, StudentsService studentsService)
                     => await studentsService.GetPrimaryGuardianAsync(id))
                .WithDefaultApiSettings( "GetPrimaryGuardians", "Guardian principal pentru elev", "GET_GUARDIAN_OPTIONS",  false   );
        }
    }
}
