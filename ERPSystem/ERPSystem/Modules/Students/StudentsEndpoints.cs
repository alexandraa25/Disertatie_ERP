using ERPSystem.Extensions;
using ERPSystem.Modules.Contracts;
using ERPSystem.Modules.Student;
using ERPSystem.Modules.Student.Models;
using ERPSystem.Shared.BusinessLogic;
using Route = ERPSystem.Utils.Constants.General.Route.Students;

namespace ERPSystem.Modules.Students
{
    public static class StudentsEndpoints
    {
        public static void Map(RouteGroupBuilder group)
        {
            group.MapGet(Route.STUDENTS,
                async (string? q, int page, int pageSize, string? sortBy, string? sortDir, int? recentDays, bool? onlyRecent, int ? sessionId, string? statusFilter, string? deleteFilter,
                       StudentsService studentsService)
                    => await studentsService.GetStudentsAsync(q, page, pageSize, sortBy, sortDir, recentDays, onlyRecent, sessionId, statusFilter, deleteFilter))
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


            group.MapPatch(Route.STUDENT_RESTORE,
                async (int id, StudentsService studentsService)
                    => await studentsService.RestoreAsync(id))
                .WithDefaultApiSettings("RestoreStudent", "Restaurare elev", "UPDATE", false);


            group.MapPatch(Route.STUDENT_TOGGLE_STATUS,
                async (int id, StudentsService studentsService)
                    => await studentsService.ToggleStatusAsync(id))
                .WithDefaultApiSettings("ToggleStudentStatus", "Activare/Dezactivare elev", "UPDATE", false);

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

            group.MapGet(Route.STUDENTS_AVAILABLE_COURSE,
                 async (int id, string? q, StudentsService service)
                     => await service.GetAvailableCoursesForStudentAsync(id, q))
                 .WithDefaultApiSettings("GetAvailableCoursesForStudent", "Lista cursurilor disponibile pentru student", "AVAILABLE_COURSES_LIST", false );

            group.MapGet(Route.STUDENTS_COURSES_BY_CONTRACT,
                async (int contractId, StudentsService service)
                    => await service.GetStudentCoursesByContractAsync(contractId))
               .WithDefaultApiSettings("GetStudentCoursesByContract", "Lista cursuri si sesiuni asociate elevului pentru contract", "GET_STUDENT_COURSES_BY_CONTRACT", false);

            group.MapGet(Route.SESSIONS,
                async (StudentsService studentsService)
                   => await studentsService.GetAllSessionsAsync() )
               .WithDefaultApiSettings("GetSessions", "Lista sesiuni", "GET", false);


            group.MapGet(Route.EXPORT,
                 async ( string? q, string? sortBy,string? sortDir, bool? onlyRecent, int? recentDays, int? sessionId, string? statusFilter, string? deleteFilter, StudentsService studentsService ) =>
                 {
                     var bytes = await studentsService.ExportStudentsExcel(  q, sortBy, sortDir, onlyRecent, recentDays, sessionId, statusFilter, deleteFilter);
             
                     return Results.File(
                         bytes,
                         "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                         $"students_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx"
                     );
                 }
             )
             .WithDefaultApiSettings("ExportStudents", "Export elevi filtrat", "GET", false);
                     }
                 }
             }
