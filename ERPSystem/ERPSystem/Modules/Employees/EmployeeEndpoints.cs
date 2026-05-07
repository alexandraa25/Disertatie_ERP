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
                 async (HttpRequest httpRequest, EmployeeService service) =>
                 {
                     var form = await httpRequest.ReadFormAsync();

                     var request = new CreateEmployeeFullRequest
                     {
                         Mode = form["mode"],
                         UserId = form["userId"],
                         FirstName = form["firstName"],
                         LastName = form["lastName"],
                         Email = form["email"],
                         HireDate = DateTime.Parse(form["hireDate"]!),
                         JobTitle = form["jobTitle"],
                         Salary = decimal.TryParse(form["salary"], out var salary) ? salary : 0,
                         ContractType = form["contractType"],
                         Notes = form["notes"],
                         PhoneNumber = form["phoneNumber"],
                         EmergencyContactName = form["emergencyContactName"],
                         EmergencyContactPhone = form["emergencyContactPhone"],
                         Street = form["street"],
                         City = form["city"],
                         Country = form["country"],
                         PostalCode = form["postalCode"],
                         IBAN = form["IBAN"],
                         BankName = form["bankName"],
                         Files = form.Files.GetFiles("Files").ToArray(),
                         DocumentTypes = form["DocumentTypes"].ToArray()
                     };

                     return await service.CreateEmployeeFullAsync(request);
                 })
              .DisableAntiforgery()
              .WithDefaultApiSettings("CreateEmployee", "Creare angajat", "CREATE_EMPLOYEE", false);

            group.MapPut(Route.UPDATE_EMPLOYEE,
                async ([FromBody] UpdateEmployeeRequest request, EmployeeService service) =>
                    await service.UpdateEmployeeAsync(request))        
                .WithDefaultApiSettings(  "UpdateEmployee", "Actualizare angajat", "UPDATE_EMPLOYEE", false);

            group.MapPost(Route.EMPLOYEE_DOCUMENT,
                async (HttpRequest request, EmployeeService service) =>
                    await service.UploadEmployeeDocuments(request))
               .DisableAntiforgery()
               .WithDefaultApiSettings("UploadEmployeeDocuments", "Încarcă document angajat", "UPLOAD_EMPLOYEE_DOCUMENTS",  false);

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


            group.MapGet(Route.EMPLOYEE_DOCUMENT_VIEW,
                async (Guid documentId, EmployeeService service)
                     => await service.ViewEmployeeDocumentAsync(documentId))
             .WithDefaultApiSettings("ViewEmployeeDocument", "View Employee DocumenT", "View Employee Document", false);

            group.MapGet(Route.EMPLOYEE_DOCUMENT_DOWNLOAD,
               async (Guid documentId, EmployeeService service)
               => await service.DownloadEmployeeDocumentAsync(documentId))
                .WithDefaultApiSettings("DownloadEmployeeDocument", "Download Employee Document", "Download Employee Document", false);

            group.MapDelete(Route.EMPLOYEE_DOCUMENT_DELETE,
               async (Guid documentId, EmployeeService service) 
                  => await service.DeleteEmployeeDocumentAsync(documentId));

            group.MapGet(Route.EXPORT_EMPLOYEES_EXCEL,
               async ( string? q, string? status, string? contractType, EmployeeService service)
                   => await service.ExportEmployeesExcelAsync(  q, status, contractType))
              .WithDefaultApiSettings( "ExportEmployeesExcel", "Exportă angajații în Excel", "EXPORT_EMPLOYEES_EXCEL", true);

        }
    }
}
