using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Feedback.Analytics.Models;
using ERPSystem.Modules.Feedback.Analytics.Models.ERPSystem.Modules.Feedback.Analytics.Models;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace ERPSystem.Modules.Feedback.Analytics
{
    public class FeedbackGlobalAnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public FeedbackGlobalAnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PublicResponse> GetGlobalAnalyticsAsync()
        {
            PublicResponse response = new(true);

            var courseReviews = await _context.CourseReviews
                .AsNoTracking()
                .ToListAsync();

            var studentEvaluations = await _context.StudentEvaluations
                .AsNoTracking()
                .ToListAsync();

            var externalReviews = await _context.ExternalReviews
                .AsNoTracking()
                .ToListAsync();

            if (!courseReviews.Any() && !studentEvaluations.Any() && !externalReviews.Any())
                return response.SetError("NoFeedbackData", "Nu există date suficiente pentru analiza globală.");

            var dto = new FeedbackGlobalAnalyticsDto
            {
                TotalCourseReviews = courseReviews.Count,
                TotalStudentEvaluations = studentEvaluations.Count,
                TotalExternalReviews = externalReviews.Count,

                AverageCourseRating = AverageOrZero(courseReviews.Select(x => x.Rating)),
                AverageStudentEvaluationRating = AverageOrZero(studentEvaluations.Select(x => x.Rating)),
                AverageExternalRating = AverageOrZero(
                    externalReviews
                        .Where(x => x.Rating.HasValue)
                        .Select(x => x.Rating!.Value)
                ),

                AveragePublicPerceptionScore = AverageOrZero(
                    externalReviews
                        .Where(x => x.PublicPerceptionScore.HasValue)
                        .Select(x => x.PublicPerceptionScore!.Value)
                ),

                AverageStudentRiskScore = AverageOrZero(
                    studentEvaluations
                        .Where(x => x.StudentRiskScore.HasValue)
                        .Select(x => x.StudentRiskScore!.Value)
                ),

                TopProblems = ExtractTopProblems(courseReviews, studentEvaluations, externalReviews),

                TopTeachers = await GetTopTeachersAsync(),
                TopCourses = await GetTopCoursesAsync()
            };

            dto.TeachersNeedingAttention = dto.TopTeachers
                .Where(x => x.AverageRating < 3 || x.NegativePercent >= 40 || x.AverageTeacherScore < 0.6)
                .OrderByDescending(x => x.NegativePercent)
                .Take(5)
                .ToList();

            dto.CoursesNeedingAttention = dto.TopCourses
                .Where(x => x.AverageRating < 3 || x.NegativePercent >= 40 || x.AverageCourseScore < 0.6)
                .OrderByDescending(x => x.NegativePercent)
                .Take(5)
                .ToList();

            dto.Alerts = GenerateAlerts(dto);
            dto.Recommendations = GenerateRecommendations(dto);
            dto.MainInsight = GenerateMainInsight(dto);
            dto.Summary = GenerateSummary(dto);

            return response.SetSuccess(dto);
        }

        private async Task<List<TopTeacherDto>> GetTopTeachersAsync()
        {
            var reviews = await _context.CourseReviews
                .AsNoTracking()
                .ToListAsync();

            var sessions = await _context.CourseSessions
                .Include(x => x.Teacher)
                .ToListAsync();

            return reviews
                .Join(
                    sessions,
                    r => r.CourseSessionId,
                    s => s.Id,
                    (r, s) => new { r, s }
                )
                .GroupBy(x => new
                {
                    x.s.TeacherUserId,
                    TeacherName = x.s.Teacher.FullName
                })
                .Select(g => new TopTeacherDto
                {
                    TeacherUserId = g.Key.TeacherUserId,
                    TeacherName = g.Key.TeacherName,
                    AverageRating = Math.Round(g.Average(x => x.r.Rating), 2),
                    AverageTeacherScore = Math.Round(g.Average(x => x.r.TeacherScore ?? 0), 2),
                    NegativePercent = AverageOrZero(
                        g.Where(x => x.r.NegativePercent.HasValue)
                         .Select(x => x.r.NegativePercent!.Value)
                    ),
                    ReviewsCount = g.Count()
                })
                .OrderByDescending(x => x.AverageRating)
                .Take(5)
                .ToList();
        }

        private async Task<List<TopCourseDto>> GetTopCoursesAsync()
        {
            var reviews = await _context.CourseReviews
                .AsNoTracking()
                .ToListAsync();

            var sessions = await _context.CourseSessions
                .Include(x => x.Course)
                .ToListAsync();

            return reviews
                .Join(
                    sessions,
                    r => r.CourseSessionId,
                    s => s.Id,
                    (r, s) => new { r, s }
                )
                .GroupBy(x => new
                {
                    x.s.Id,
                    CourseName = x.s.Course.Name
                })
                .Select(g => new TopCourseDto
                {
                    CourseSessionId = g.Key.Id,
                    CourseName = g.Key.CourseName,
                    AverageRating = Math.Round(g.Average(x => x.r.Rating), 2),
                    AverageCourseScore = Math.Round(g.Average(x => x.r.CourseScore ?? 0), 2),
                    NegativePercent = AverageOrZero(
                        g.Where(x => x.r.NegativePercent.HasValue)
                         .Select(x => x.r.NegativePercent!.Value)
                    ),
                    ReviewsCount = g.Count()
                })
                .OrderByDescending(x => x.AverageRating)
                .Take(5)
                .ToList();
        }

        private List<TopicSummaryDto> ExtractTopProblems(
            List<CourseReview> courseReviews,
            List<StudentEvaluation> studentEvaluations,
            List<ExternalReview> externalReviews)
        {
            var topicCounts = new Dictionary<string, int>();

            var jsonValues = new List<string?>();

            jsonValues.AddRange(courseReviews.Select(x => x.TopicsJson));
            jsonValues.AddRange(studentEvaluations.Select(x => x.TopicsJson));
            jsonValues.AddRange(externalReviews.Select(x => x.TopicsJson));

            foreach (var json in jsonValues.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                try
                {
                    var topics = JArray.Parse(json!);

                    foreach (var topic in topics)
                    {
                        var name = topic["name"]?.ToString();

                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        topicCounts[name] = topicCounts.GetValueOrDefault(name) + 1;
                    }
                }
                catch
                {
                }
            }

            var total = topicCounts.Values.Sum();

            if (total == 0)
                return new List<TopicSummaryDto>();

            return topicCounts
                .OrderByDescending(x => x.Value)
                .Take(10)
                .Select(x => new TopicSummaryDto
                {
                    Name = x.Key,
                    Count = x.Value,
                    Percent = Math.Round((double)x.Value / total * 100, 2)
                })
                .ToList();
        }

        private List<string> GenerateAlerts(FeedbackGlobalAnalyticsDto dto)
        {
            var alerts = new List<string>();

            if (dto.AverageStudentRiskScore >= 50)
                alerts.Add("Riscul mediu al cursanților este ridicat.");

            if (dto.AveragePublicPerceptionScore > 0 && dto.AveragePublicPerceptionScore < 0.5)
                alerts.Add("Percepția publică este sub nivelul recomandat.");

            if (dto.CoursesNeedingAttention.Any())
                alerts.Add("Există cursuri care necesită intervenție.");

            if (dto.TeachersNeedingAttention.Any())
                alerts.Add("Există profesori cu feedback sub nivelul recomandat.");

            return alerts;
        }

        private List<string> GenerateRecommendations(FeedbackGlobalAnalyticsDto dto)
        {
            var recommendations = new List<string>();

            if (dto.TopProblems.Any(x => x.Name == "Ritm curs"))
                recommendations.Add("Analizează ritmul cursurilor și introdu sesiuni de recapitulare unde este necesar.");

            if (dto.TopProblems.Any(x => x.Name == "Materiale"))
                recommendations.Add("Revizuiește materialele de curs și adaugă resurse suplimentare.");

            if (dto.TopProblems.Any(x => x.Name == "Calitate profesor"))
                recommendations.Add("Monitorizează claritatea explicațiilor și suportul oferit de profesori.");

            if (dto.AverageStudentRiskScore >= 50)
                recommendations.Add("Prioritizează cursanții cu risc ridicat pentru intervenții individuale.");

            if (!recommendations.Any())
                recommendations.Add("Feedbackul global este stabil. Continuă monitorizarea periodică.");

            return recommendations;
        }

        private string GenerateMainInsight(FeedbackGlobalAnalyticsDto dto)
        {
            if (dto.AverageStudentRiskScore >= 70)
                return "Platforma indică un risc ridicat la nivelul cursanților.";

            if (dto.CoursesNeedingAttention.Any())
                return $"Cursul cu cel mai mare risc este: {dto.CoursesNeedingAttention.First().CourseName}.";

            if (dto.TeachersNeedingAttention.Any())
                return $"Profesorul care necesită atenție este: {dto.TeachersNeedingAttention.First().TeacherName}.";

            if (dto.TopProblems.Any())
                return $"Problema dominantă în feedback este: {dto.TopProblems.First().Name}.";

            return "Feedbackul global nu indică probleme critice.";
        }

        private string GenerateSummary(FeedbackGlobalAnalyticsDto dto)
        {
            var total =
                dto.TotalCourseReviews +
                dto.TotalStudentEvaluations +
                dto.TotalExternalReviews;

            return $"Analiza globală include {total} feedbackuri/evaluări. Ratingul mediu al cursurilor este {dto.AverageCourseRating}/5, iar scorul mediu de risc al cursanților este {dto.AverageStudentRiskScore}%.";
        }

        private static double AverageOrZero(IEnumerable<int> values)
        {
            var list = values.ToList();
            return list.Any() ? Math.Round(list.Average(), 2) : 0;
        }

        private static double AverageOrZero(IEnumerable<double> values)
        {
            var list = values.ToList();
            return list.Any() ? Math.Round(list.Average(), 2) : 0;
        }
    }
}