using ERPSystem.Extensions;
using ERPSystem.Modules.Admin;
using ERPSystem.Modules.Employees;
using ERPSystem.Modules.Employees.Models;

using ERPSystem.Shared.BusinessLogic;
using Microsoft.AspNetCore.Mvc;
using Route = ERPSystem.Utils.Constants.General.Route.Employee;

namespace ERPSystem.Modules.Employees
{
    public class EmployeeEndpoints
    {
        public static void Map(RouteGroupBuilder group)
        {
            
            group.MapPost(Route.EMPLOYEE,
               async ([FromForm] CreateEmployeeFullRequest request, EmployeeService service) =>
                    await service.CreateEmployeeFullAsync(request))
                .DisableAntiforgery()
                 .WithDefaultApiSettings("CreateEmployee", "Creare angajat", "CREATE_EMPLOYEE", false);

            group.MapPut(Route.UPDATE_EMPLOYEE,
                async ([FromBody] UpdateEmployeeRequest request, EmployeeService service) =>
                    await service.UpdateEmployeeAsync(request))        
                .WithDefaultApiSettings(  "UpdateEmployee", "Actualizare angajat", "UPDATE_EMPLOYEE", false);

            group.MapPost(Route.EMPLOYEE_DOCUMENT,
                async ([FromForm] UploadEmployeeDocsRequest request, EmployeeService service)
                    => await service.UploadEmployeeDocuments(request))
                .WithDefaultApiSettings("UploadEmployeeDocuments", "Incarcare documente angajat", "EMPLOYEE_DOCUMENT", false)
                .DisableAntiforgery();
               
            group.MapGet(Route.USERS,
                  async (EmployeeService service)
                    => await service.GetSimpleUsers())
                 .WithDefaultApiSettings("GetSimplUsers", "User", "GET_SIMPLE_USERS", false);

            group.MapGet(Route.EMPLOYEES,
                async ([AsParameters] EmployeeListRequest request, EmployeeService service) 
                  => await service.GetEmployeesAsync(request))
                .WithDefaultApiSettings("ListEmployee", " Employees", "List_EMPLOYEE", false);

            group.MapGet(Route.EMPLOYEE_BY_ID,
                async (Guid id, EmployeeService service)
                    => await service.GetEmployeeAsync(id))
                .WithDefaultApiSettings("getidEmployee", " Employee by id", "EMPLOYEE_BY_ID", false);

            group.MapPost(Route.TERMINATE_EMPLOYEE,
                async (Guid id, [FromForm] TerminateEmployeeRequest request, EmployeeService service)
                    => await service.TerminateEmployeeAsync(id, request))
                .DisableAntiforgery()
                .WithDefaultApiSettings("TerminateEmployee", "Terminate Employee", "Terminate_EMPLOYEE", false);

            group.MapGet(Route.HR_DASHBOARD,
                async (EmployeeService service)
                    => await service.GetDashboardAsync())
                .WithDefaultApiSettings("HRDASHBOARD", "HRDASHBOARD", "HR_DASHBOARD", true);


        }
    }
}
