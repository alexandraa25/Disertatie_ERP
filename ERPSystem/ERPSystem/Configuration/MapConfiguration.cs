using ERPSystem.Extensions;
using ERPSystem.Modules.Authentificate;
using ERPSystem.Modules.Courses;
using ERPSystem.Modules.Students;
using ERPSystem.Modules.UserProfile;
using ERPSystem.Modules.Contracts;

namespace ERPSystem.Configuration
{
    public static class MapConfiguration
    {
        public static void MapAllApiEndpoints(this WebApplication app)
        {
            var authGroup = app.CreateApiGroup(
                route: "/auth",
                tag: "Authentication",
                requireAuth: false,
                description: "Authentication & Identity endpoints"
            );

            AuthenticationEndpoints.Map(authGroup);


            // ✅ GROUP pentru profil (necesită autentificare)
            var meGroup = app.CreateApiGroup(
                route: "/me",
                tag: "Me",
                requireAuth: true,
                description: "Current user profile, preferences & activity"
            );

            UserProfileEndpoints.Map(meGroup);

            var studentsGroup = app.CreateApiGroup(
                route: "/students",
                tag: "Students",
                requireAuth: false,
                description: "Students CRUD endpoints"
             );

            StudentsEndpoints.Map(studentsGroup);

            var coursesGroup = app.CreateApiGroup(
                route: "/courses",
                tag: "Courses",
                requireAuth: false, // temporar, ca la students
                description: "Courses endpoints"
             );

            CoursesEndpoints.Map(coursesGroup);
            
            var contractsGroup = app.CreateApiGroup(
               route: "/contracts",
               tag: "Contracts",
               requireAuth: false, // temporar, ca la students
               description: "Contracts endpoints"
            );

            ContractsEndpoints.Map(contractsGroup);
        }
    }
}
