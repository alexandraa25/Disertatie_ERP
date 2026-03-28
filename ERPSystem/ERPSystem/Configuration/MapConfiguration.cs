using ERPSystem.Extensions;
using ERPSystem.Modules.AdditionalAct;
using ERPSystem.Modules.Admin;
using ERPSystem.Modules.Authentificate;
using ERPSystem.Modules.Contracts;
using ERPSystem.Modules.Courses;
using ERPSystem.Modules.Employees;
using ERPSystem.Modules.Payments;
using ERPSystem.Modules.Students;
using ERPSystem.Modules.UserProfile;
using ERPSystem.Shared.ActivityLogs;

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


            var generalGroup = app.CreateApiGroup(
               route: "/",
               tag: "General",
               requireAuth: false,
               description: "Audit"
           );

            ActivityLogEndpoint.Map(generalGroup);


            // ✅ GROUP pentru profil (necesită autentificare)
            var meGroup = app.CreateApiGroup(
                route: "/me",
                tag: "Me",
                requireAuth: true,
                description: "Current user profile, preferences & activity"
            );

            UserProfileEndpoints.Map(meGroup);

            var adminGroup = app.CreateApiGroup(
                route: "/admin",
                tag: "Admin",
                requireAuth: false,
                description: "Admin dashboard"
            );
            AdminEndpoints.Map(adminGroup);
         


            var employeeGroup = app.CreateApiGroup(
                route: "/employee",
                tag: "Employee",
                requireAuth: false,
                description: "Employee dashboard"
            );

            EmployeeEndpoints.Map(employeeGroup);

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

            var additionalActGroup = app.CreateApiGroup(
               route: "/additional-act",
               tag: "AdditionalAct",
               requireAuth: false, // temporar, ca la students
               description: "AdditionalAct endpoints"
            );

            AdditionalActEndpoints.Map(additionalActGroup);

            var paymentsGroup = app.CreateApiGroup(
              route: "",
              tag: "Payments",
              requireAuth: false, // temporar, ca la students
              description: "Payments endpoints"
           );

            PaymentsEndpoints.Map(paymentsGroup);

            var dashboardGroup = app.CreateApiGroup(
              route: "/dashboard",
              tag: "Dashboard",
              requireAuth: false, // temporar, ca la students
              description: "Dashboard endpoints"
           );

            DashboardEndpoints.Map(dashboardGroup);


            var companyGroup = app.CreateApiGroup(
             route: "/company",
             tag: "company",
             requireAuth: false, // temporar, ca la students
             description: "Company endpoints"
          );

            CompanyEndpoints.Map(companyGroup);
        }
    }
}
