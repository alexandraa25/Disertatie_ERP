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
            

            public static class Students
            {
                public const string STUDENTS = "";          // list + create
                public const string STUDENT_BY_ID = "/{id:int}";
                public const string STUDENT_OPTIONS = "/students/options";

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


        }

        public static class Status
        {
            public const string ACTIVE = "ACTIVE";
            public const string INACTIVE = "INACTIVE";
        }
    }
}
