namespace ERPSystem.Shared.Notifications
{
    public static class NotificationEvents
    {
        public const string FeedbackSubmitted = "FeedbackSubmitted";
        public const string FormSubmitted = "FormSubmitted";
        public const string ReportGenerated = "ReportGenerated";
        public const string UserActivity = "UserActivity";
        public const string SystemUpdate = "SystemUpdate";
        public const string Leave = "Leave";
        public const string Employee = "Employee";
        public const string CourseActivity = "CourseActivity";
        public const string StudentActivity = "StudentActivity";
        public const string ContractActivity = "ContractActivity";


        public static readonly string[] All =
        {
            FeedbackSubmitted,
            FormSubmitted,
            ReportGenerated,
            UserActivity,
            SystemUpdate,
            Leave,
            Employee,
            StudentActivity,
            ContractActivity
        };
    }
}
