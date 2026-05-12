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
        public const string Feedback = "Feedback";
        public const string MarketingActivity = "MarketingActivity";


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
            ContractActivity,
            Feedback,
            MarketingActivity
        };


        public static readonly Dictionary<string, string[]> ByRole = new()
        {
            ["Admin"] = new[]
        {
        UserActivity,
        SystemUpdate,
        Leave,
        Employee,
        CourseActivity,
        StudentActivity,
        ContractActivity,
        Feedback,
        MarketingActivity
        },

            ["Manager"] = new[]
        {
        UserActivity,
        Leave,
        Employee,
        CourseActivity,
        StudentActivity,
        ContractActivity,
        Feedback,
        MarketingActivity
    },

            ["HR"] = new[]
        {
        Leave,
        Employee
    },

            ["Secretary"] = new[]
        {
        StudentActivity,
        CourseActivity,
        ContractActivity,
        Feedback,
        Leave,
        Employee
    },

            ["Teacher"] = new[]
        {
        CourseActivity,
        StudentActivity,
        Feedback,
        Leave
    },

            ["Accountant"] = new[]
        {
        ContractActivity
    },

            ["Marketing"] = new[]
        {
        MarketingActivity,
        Feedback
    }
        };
    }
}


