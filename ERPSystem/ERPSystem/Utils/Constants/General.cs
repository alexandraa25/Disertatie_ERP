using Azure.Core;
using ERPSystem.Modules.Employees;
using ERPSystem.Modules.Employees.Models;

namespace ERPSystem.Utils.Constants
{
    public class General
    {
        public class Route
        {
            public const string REGISTER = "register";
            public const string CHECK_USER_EXISTENCE = "check-user-existence";
            public const string CONFIRM_EMAIL_REGISTRATION = "confirm-email-registration";
            public const string LOGIN = "login";
            public const string CONFIRM_LOGIN_CODE = "confirm-login-code";
            public const string RESEND_LOGIN_CODE = "resend-login-code";
            public const string FORGOT_PASSWORD = "forgot-password";
            public const string RESET_PASSWORD = "reset-password";
            public const string GET_ROLES = "/roles";
            public const string CHANGE_PASSWORD = "change-password";

            public static class General
            {
                public const string ACTIVITY = "/activity";
                public const string ACTIVITY_ALL = "/activity/all";
                public const string FILLTER_OPTIONS = "/fillter";
            }

            public static class Profile
            {
                public const string PROFILE = "/profile";
                public const string NOTIFICATION_SETTINGS = "/notification-settings";

            }

            public static class Admin
            {
                public const string USERS = "/users";
                public const string EMPLOYEES_WITHOUY_USER = "/employees-without-user"; 

            }

            public static class Company
            {
                public const string COMPANY_GET = "/";
                public const string COMPANY_SAVE = "/";

            }


            public static class Employee
            {
                public const string EMPLOYEE = "";
                public const string UPDATE_EMPLOYEE = "/update"; 
                public const string EMPLOYEE_DOCUMENT = "/upload-documents";
                public const string EMPLOYEES = "/list";
                public const string EMPLOYEE_BY_ID = "/{id}";
                public const string TERMINATE_EMPLOYEE = "/{id}/terminate";
                public const string HR_DASHBOARD = "/dashboard";
                public const string USERS = "/users";

            }

            public static class Leaves
            {
                public const string MY_LEAVES = "";
                public const string CREATE_LEAVES = "/create";
                public const string UPDATE_LEAVES = "/{id}";
                public const string CANCEL_LEAVES = "/{id}/cancel";
                public const string APPROVE_LEAVES = "/{id}/approve";
                public const string REJECT_LEAVES = "/{id}/reject";
                public const string ALL_LEAVES = "/all";
                public const string LEAVES_BY_USER = "";
                public const string HOLIDAYS = "/holidays";
                public const string GET_CONFLICTS = "/conflicts";
                public const string EXPORT_LEAVES = "/export";
              

            }

            public static class Students
            {
                public const string STUDENTS = "";          
                public const string STUDENT_BY_ID = "/{id:int}";
                public const string STUDENT_OPTIONS = "/students/options";
                public const string STUDENT_COURSES = "/{id}/courses";
                public const string GUARDIAN_OPTIONS = "/{id}/primary-guardian";
                public const string STUDENTS_AVAILABLE_COURSE = "{id}/available-courses";
                public const string STUDENTS_COURSES_BY_CONTRACT = "by-contract/{contractId}";
                public const string SESSIONS = "/sessions";
                public const string EXPORT = "/ /export";

              

            }



            public static class Courses
            {
                public const string COURSES = "";
                public const string COURSE_BY_ID = "/{id:int}";
                public const string COURSE_ENROLLMENTS = "/{id:int}/enrollments";
                public const string COURSE_ENROLLMENT_BY_SESSION_STUDENT = "/{id:int}/enrollments/{sessionId:int}/{studentId:int}";
                public const string COURSE_TEACHERS = "/teachers";
                public const string COURSE_AVAILABLE_STUDENTS ="{id}/sessions/{sessionId}/available-students";

            }

            public static class Contracts
            {
                public const string CONTRACTS = ""; //create
                public const string CONTRACT_UPDATE = "/{id}";
                public const string CONTRACT_UPDATE_BODY = "/{id}/body";
                public const string RESET_BODY = "/{id}/reset-body";
                public const string CONTRACT_FINALIZE = "/{id}/finalize";
                public const string CONTRACT_SEND_TO_CLIENT = "{id}/send";
                public const string DOCUMENT_CLIENT_SIGN = "/client-sign";
                public const string DOCUMENT_GET_FOR_SIGNING = "/sign/{token}";
                public const string CONTRACT_ADMIN_SIGN = "/{id}/admin-sign";
                public const string CONTRACT_ACTIVATE = "/{id:int}/activate";
                public const string CONTRACT_GENERATE_PDF = "/{id}/generate-pdf";
                public const string CONTRACT_CANCEL = "/{id}/cancel";
                public const string CONTRACT_SUSPEND = "/{id}/suspend";
                public const string CONTRACT_COMPLETE = "/{id}/complete";
                public const string CONTRACT_EXPIRE = "/expire-all";
                public const string CONTRACT_DOWNLOAD = "/{id}/download";
                public const string CONTRACT_BY_ID = "/{id:int}";
                public const string STUDENT_CONTRACTS = "/students/{id:int}";
            }
           public static class AdditionalAct
           {
                public const string CREATE_ACT = "/contract/{contractId}/create";
                public const string UPDATE_ACT = "/{id}/update";
                public const string UPDATE_BODY = "/{id}/body";
                public const string LIST_ACT = "/contract/{contractId}";
                public const string GET_BY_ID = "/{id}";
                public const string FINALIZE_ACT = "/{id}/finalize";
                public const string ACT_SEND_TO_CLIENT = "{id}/send";
                public const string ACT_ADMIN_SIGN = "/{id}/admin-sign";
                public const string DOWNLOAD_ACT = "/{id}/download";
            }

            public static class Payments
            {
                public const string PAY_INSTALLMENT = "/installments/pay";
                public const string GET_INSTALLMENTS = "/contracts/{id}/installments";
                public const string GET_PAYMENTS = "/contracts/{id}/payments";

            }


            public static class Dashboard
            {
                public const string DASHBOARD = "";
                

            }


        }

        public static class Status
        {
            public const string ACTIVE = "ACTIVE";
            public const string INACTIVE = "INACTIVE";
        }
    }
}
