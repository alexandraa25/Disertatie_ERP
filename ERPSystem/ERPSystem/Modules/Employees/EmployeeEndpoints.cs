using ERPSystem.Extensions;
using ERPSystem.Modules.Employees;
using ERPSystem.Modules.Employees.Models;

using ERPSystem.Shared.BusinessLogic;
using Route = ERPSystem.Utils.Constants.General.Route.Employee;

namespace ERPSystem.Modules.Employees
{
    public class EmployeeEndpoints
    {
        public static void Map(RouteGroupBuilder group)
        {
            // ===============================
            // CREATE EMPLOYEE
            // ===============================
            group.MapPost(Route.EMPLOYEE,
                async (CreateEmployeeRequest request, EmployeeService service)
                    => await service.CreateEmployeeAsync(request))
                .WithDefaultApiSettings("CreateEmployee", "Create Employee", "CREATE_EMPLOYEE", false);


            // ===============================
            // GET EMPLOYEES
            // ===============================
            group.MapGet(Route.EMPLOYEES,
                async (EmployeeService service)
                    => await service.GetEmployeesAsync())
                .WithDefaultApiSettings("ListEmployee", " Employees", "List_EMPLOYEE", false);


            // ===============================
            // GET EMPLOYEE BY ID
            // ===============================
            group.MapGet(Route.EMPLOYEE_BY_ID,
                async (Guid id, EmployeeService service)
                    => await service.GetEmployeeAsync(id))
                .WithDefaultApiSettings("getidEmployee", " Employee by id", "EMPLOYEE_BY_ID", false);


            // ===============================
            // TERMINATE EMPLOYEE
            // ===============================
            group.MapPost(Route.TERMINATE_EMPLOYEE,
                async (Guid id, TerminateEmployeeRequest request, EmployeeService service)
                    => await service.TerminateEmployeeAsync(id, request))
                .WithDefaultApiSettings("TerminateEmployee", "Terminate Employee", "Terminate_EMPLOYEE", false);


            // ===============================
            // HR DASHBOARD
            // ===============================
            group.MapGet(Route.HR_DASHBOARD,
                async (EmployeeService service)
                    => await service.GetDashboardAsync())
                .WithDefaultApiSettings("HRDASHBOARD", "HRDASHBOARD", "HR_DASHBOARD", true);


        }
    }
}
