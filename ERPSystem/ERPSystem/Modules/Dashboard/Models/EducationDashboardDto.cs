namespace ERPSystem.Modules.Dashboard.Models;

public class EducationDashboardDto
{
    public int ActiveStudents { get; set; }
    public int ActiveCourses { get; set; }
    public int ActiveSessions { get; set; }
    public int ActiveEnrollments { get; set; }

    public int CompletedFeedbackForms { get; set; }
    public int PendingFeedbackForms { get; set; }

    public double AverageCourseRating { get; set; }
    public double AverageStudentEvaluation { get; set; }

    public List<CourseEnrollmentStatsDto> EnrollmentsByCourse { get; set; } = new();
    public List<CourseRatingStatsDto> RatingByCourse { get; set; } = new();
    public List<StudentEvaluationStatsDto> StudentEvaluationsByMonth { get; set; } = new();
}

public class CourseEnrollmentStatsDto
{
    public string CourseName { get; set; } = "";
    public int Count { get; set; }
}

public class CourseRatingStatsDto
{
    public string CourseName { get; set; } = "";
    public double AverageRating { get; set; }
}

public class StudentEvaluationStatsDto
{
    public string Month { get; set; } = "";
    public double AverageRating { get; set; }
}