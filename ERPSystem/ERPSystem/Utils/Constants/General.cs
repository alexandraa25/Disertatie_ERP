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

            public static class Profile
            {
                public const string PROFILE = "/profile";
                public const string NOTIFICATION_SETTINGS = "/notification-settings";
               
            }

            public static class Admin
            {
                public const string USERS = "/users";

            }

            public static class Company
            {
                public const string COMPANY_GET = "/";
                public const string COMPANY_SAVE = "/";

            }


            public static class Employee
            {
                public const string EMPLOYEE = "/employee";
                public const string EMPLOYEES = "/employees";
                public const string EMPLOYEE_BY_ID = "/employee/{id}";
                public const string TERMINATE_EMPLOYEE = "/employee/{id}/terminate";
                public const string HR_DASHBOARD = "/employees/dashboard";

            }

            public static class Students
            {
                public const string STUDENTS = "";          // list + create
                public const string STUDENT_BY_ID = "/{id:int}";
                public const string STUDENT_OPTIONS = "/students/options";
                public const string STUDENT_COURSES = "/{id}/courses";
                public const string GUARDIAN_OPTIONS = "/{id}/primary-guardian";
                public const string STUDENTS_AVAILABLE_COURSE = "{id}/available-courses";

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
                public const string CONTRACTS = "";
                public const string CONTRACT_BY_ID = "/{id:int}";
                public const string CONTRACT_ACTIVATE = "/contracts/{id:int}/activate";
                public const string STUDENT_CONTRACTS = "/students/{id:int}/contracts";
                public const string CONTRACT_FINALIZE = "/{id}/finalize";
                public const string CONTRACT_SIGN = "/{id}/sign";
                public const string CONTRACT_CANCEL = "/{id}/cancel";
                public const string CONTRACT_UPDATE_BODY = "/{id}/body";
                public const string CONTRACT_GENERATE_PDF = "/{id}/generate-pdf";
                public const string CONTRACT_GET_LATEST_BY_STUDENT ="/latest-by-student/{studentId:int}";
                public const string CONTRACT_SEND_TO_CLIENT = "{id}/send";
                public const string CONTRACT_CLIENT_SIGN = "/client-sign";
                public const string CONTRACT_GET_FOR_SIGNING = "/sign/{token}";
                public const string CONTRACT_ADMIN_SIGN = "/contracts/{id}/admin-sign";




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
