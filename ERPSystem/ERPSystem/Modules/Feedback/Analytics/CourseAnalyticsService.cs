using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Feedback.Analytics.Models;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace ERPSystem.Modules.Feedback.Analytics
{
    public class CourseAnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public CourseAnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PublicResponse> GetCourseAnalyticsAsync(int courseSessionId)
            {
                PublicResponse response = new(true);

                var sessionExists = await _context.CourseSessions
                    .AnyAsync(x => x.Id == courseSessionId);

                if (!sessionExists)
                    return response.SetError("SessionNotFound", "Sesiunea nu a fost găsită.");

                var courseReviews = await _context.CourseReviews
                    .Where(x => x.CourseSessionId == courseSessionId)
                    .ToListAsync();

                var studentEvaluations = await _context.StudentEvaluations
                    .Where(x => x.CourseSessionId == courseSessionId)
                    .ToListAsync();

                var allPositive = new List<int>();
                var allNegative = new List<int>();
                var allNeutral = new List<int>();

                allPositive.AddRange(courseReviews.Where(x => x.PositivePercent.HasValue).Select(x => x.PositivePercent!.Value));
                allPositive.AddRange(studentEvaluations.Where(x => x.PositivePercent.HasValue).Select(x => x.PositivePercent!.Value));

                allNegative.AddRange(courseReviews.Where(x => x.NegativePercent.HasValue).Select(x => x.NegativePercent!.Value));
                allNegative.AddRange(studentEvaluations.Where(x => x.NegativePercent.HasValue).Select(x => x.NegativePercent!.Value));

                allNeutral.AddRange(courseReviews.Where(x => x.NeutralPercent.HasValue).Select(x => x.NeutralPercent!.Value));
                allNeutral.AddRange(studentEvaluations.Where(x => x.NeutralPercent.HasValue).Select(x => x.NeutralPercent!.Value));

                var averageRating = CalculateAverageRating(courseReviews, studentEvaluations);
                var courseScore = CalculateCourseScore(courseReviews, studentEvaluations);
                var topProblems = ExtractTopTopics(courseReviews, studentEvaluations);

                var dto = new CourseAnalyticsDto
                {
                    CourseSessionId = courseSessionId,
                    AverageRating = averageRating,
                    CourseScore = courseScore,

                    PositivePercent = allPositive.Any() ? Math.Round(allPositive.Average(), 2) : 0,
                    NegativePercent = allNegative.Any() ? Math.Round(allNegative.Average(), 2) : 0,
                    NeutralPercent = allNeutral.Any() ? Math.Round(allNeutral.Average(), 2) : 0,

                    TopProblems = topProblems,
                    Trend = CalculateTrend(courseReviews, studentEvaluations)
                };

                dto.Alerts = GenerateAlerts(dto);
                dto.Recommendations = GenerateRecommendations(dto.TopProblems);
                dto.Summary = GenerateSummary(dto);
                dto.MainInsight = GenerateMainInsight(dto);

                return response.SetSuccess(dto);
            }

        public async Task<PublicResponse> GetStudentAnalyticsAsync(int studentId)
            {
                PublicResponse response = new(true);

                var studentExists = await _context.Students.AnyAsync(x => x.Id == studentId);

                if (!studentExists)
                    return response.SetError("StudentNotFound", "Cursantul nu a fost găsit.");

                var evaluations = await _context.StudentEvaluations
                    .Where(x => x.StudentId == studentId)
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();

                if (!evaluations.Any())
                    return response.SetError("NoEvaluations", "Nu există evaluări pentru acest cursant.");

                var topProblems = ExtractStudentTopTopics(evaluations);

                var dto = new StudentAnalyticsDto
                {
                    StudentId = studentId,

                    AverageRating = Math.Round(evaluations.Average(x => x.Rating), 2),

                    AverageAttendanceScore = Math.Round(
                        evaluations.Where(x => x.AttendanceScore.HasValue)
                            .Select(x => x.AttendanceScore!.Value)
                            .DefaultIfEmpty(0)
                            .Average(), 2),

                    AverageBehaviorScore = Math.Round(
                        evaluations.Where(x => x.BehaviorScore.HasValue)
                            .Select(x => x.BehaviorScore!.Value)
                            .DefaultIfEmpty(0)
                            .Average(), 2),

                    AverageProgressScore = Math.Round(
                        evaluations.Where(x => x.ProgressScore.HasValue)
                            .Select(x => x.ProgressScore!.Value)
                            .DefaultIfEmpty(0)
                            .Average(), 2),

                    AverageRiskScore = Math.Round(
                        evaluations.Where(x => x.StudentRiskScore.HasValue)
                            .Select(x => x.StudentRiskScore!.Value)
                            .DefaultIfEmpty(0)
                            .Average(), 2),

                    PositivePercent = Math.Round(
                        evaluations.Where(x => x.PositivePercent.HasValue)
                            .Select(x => x.PositivePercent!.Value)
                            .DefaultIfEmpty(0)
                            .Average(), 2),

                    NegativePercent = Math.Round(
                        evaluations.Where(x => x.NegativePercent.HasValue)
                            .Select(x => x.NegativePercent!.Value)
                            .DefaultIfEmpty(0)
                            .Average(), 2),

                    NeutralPercent = Math.Round(
                        evaluations.Where(x => x.NeutralPercent.HasValue)
                            .Select(x => x.NeutralPercent!.Value)
                            .DefaultIfEmpty(0)
                            .Average(), 2),

                    TopProblems = topProblems,
                    Trend = CalculateStudentTrend(evaluations)
                };

                dto.Alerts = GenerateStudentAlerts(dto);
                dto.Recommendations = GenerateStudentRecommendations(dto);
                dto.Summary = GenerateStudentSummary(dto);
                dto.MainInsight = GenerateStudentMainInsight(dto);

                return response.SetSuccess(dto);
            }

        private double CalculateAverageRating( List<Data.Entities.CourseReview> courseReviews, List<Data.Entities.StudentEvaluation> studentEvaluations)
            {
                var ratings = new List<int>();

                ratings.AddRange(courseReviews.Select(x => x.Rating));
                ratings.AddRange(studentEvaluations.Select(x => x.Rating));

                return ratings.Any() ? Math.Round(ratings.Average(), 2) : 0;
            }

        private double CalculateCourseScore( List<CourseReview> courseReviews, List<StudentEvaluation> studentEvaluations)
        {
            var studentFeedbackScore = courseReviews.Any()
                ? courseReviews.Average(x => x.Rating)
                : 0;

            var teacherFeedbackScore = studentEvaluations.Any()
                ? studentEvaluations.Average(x => x.Rating)
                : 0;

            var score =
                studentFeedbackScore * 0.7 +
                teacherFeedbackScore * 0.3;

            return Math.Round(score, 2);
        }

        private List<TopicSummaryDto> ExtractTopTopics( List<CourseReview> courseReviews,List<StudentEvaluation> studentEvaluations)
        {
            var topicCounts = new Dictionary<string, int>();

            var topicsJsonValues = new List<string?>();

            topicsJsonValues.AddRange(courseReviews.Select(x => x.TopicsJson));
            topicsJsonValues.AddRange(studentEvaluations.Select(x => x.TopicsJson));

            foreach (var json in topicsJsonValues.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                try
                {
                    var topics = JArray.Parse(json!);

                    foreach (var topic in topics)
                    {
                        var name = topic["name"]?.ToString();

                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        if (!topicCounts.ContainsKey(name))
                            topicCounts[name] = 0;

                        topicCounts[name]++;
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
                .Take(5)
                .Select(x => new TopicSummaryDto
                {
                    Name = x.Key,
                    Count = x.Value,
                    Percent = Math.Round((double)x.Value / total * 100, 2)
                })
                .ToList();
        }

        private List<string> GenerateAlerts(CourseAnalyticsDto dto)
            {
                var alerts = new List<string>();

                if (dto.NegativePercent > 40)
                    alerts.Add("Sentimentul negativ depășește 40%.");

                if (dto.AverageRating > 0 && dto.AverageRating < 3)
                    alerts.Add("Ratingul mediu este sub 3.");

                if (dto.TopProblems.Any(x => x.Name == "Ritm curs"))
                    alerts.Add("Există probleme frecvente legate de ritmul cursului.");

                return alerts;
            }

        private List<string> GenerateRecommendations(List<TopicSummaryDto> topics)
            {
                var recommendations = new List<string>();

                if (topics.Any(x => x.Name == "Ritm curs"))
                    recommendations.Add("Reduce ritmul cursului sau adaugă sesiuni de recapitulare.");

                if (topics.Any(x => x.Name == "Materiale"))
                    recommendations.Add("Îmbunătățește materialele de curs și adaugă exemple suplimentare.");

                if (topics.Any(x => x.Name == "Exerciții practice"))
                    recommendations.Add("Adaugă mai multe exerciții practice și aplicații.");

                if (topics.Any(x => x.Name == "Calitate profesor"))
                    recommendations.Add("Analizează claritatea explicațiilor și nivelul de suport oferit de profesor.");

                return recommendations;
            }

        private string GenerateSummary(CourseAnalyticsDto dto)
            {
                if (dto.AverageRating == 0 && !dto.TopProblems.Any())
                    return "Nu există suficiente date pentru generarea unui rezumat.";

                var mainProblem = dto.TopProblems.FirstOrDefault()?.Name;

                if (dto.NegativePercent > 40 && mainProblem != null)
                {
                    return $"Feedback-ul indică un nivel ridicat de nemulțumire. Principala problemă identificată este '{mainProblem}', iar ratingul mediu este {dto.AverageRating}/5.";
                }

                if (dto.PositivePercent > dto.NegativePercent && mainProblem != null)
                {
                    return $"Feedback-ul este predominant pozitiv, cu un rating mediu de {dto.AverageRating}/5. Totuși, tema '{mainProblem}' apare frecvent în comentarii și ar trebui monitorizată.";
                }

                if (dto.PositivePercent > 60)
                {
                    return $"Cursul are o percepție generală pozitivă, cu {dto.PositivePercent}% sentiment pozitiv și rating mediu {dto.AverageRating}/5.";
                }

                if (dto.NegativePercent > dto.PositivePercent)
                {
                    return $"Feedback-ul sugerează nemulțumiri recurente. Este recomandată analiza detaliată a comentariilor și a temelor identificate.";
                }

                return $"Feedback-ul este mixt. Ratingul mediu este {dto.AverageRating}/5, iar principalele teme identificate trebuie urmărite în evoluție.";
            }

        private List<TrendDto> CalculateTrend( List<CourseReview> courseReviews,List<StudentEvaluation> studentEvaluations)
        {
            var items = new List<(DateTime CreatedAt, int? Rating, int? PositivePercent, int? NegativePercent)>();

            items.AddRange(courseReviews.Select(x =>
                (x.CreatedAt, (int?)x.Rating, x.PositivePercent, x.NegativePercent)));

            items.AddRange(studentEvaluations.Select(x =>
                (x.CreatedAt, (int?)x.Rating, x.PositivePercent, x.NegativePercent)));

            return items
                .Where(x => x.Rating.HasValue)
                .GroupBy(x => new
                {
                    x.CreatedAt.Year,
                    x.CreatedAt.Month
                })
                .OrderBy(x => x.Key.Year)
                .ThenBy(x => x.Key.Month)
                .Select(g => new TrendDto
                {
                    Month = $"{g.Key.Month:00}.{g.Key.Year}",
                    AverageRating = Math.Round(g.Average(x => x.Rating!.Value), 2),
                    PositivePercent = Math.Round(
                        g.Where(x => x.PositivePercent.HasValue)
                            .Select(x => x.PositivePercent!.Value)
                            .DefaultIfEmpty(0)
                            .Average(), 2),
                    NegativePercent = Math.Round(
                        g.Where(x => x.NegativePercent.HasValue)
                            .Select(x => x.NegativePercent!.Value)
                            .DefaultIfEmpty(0)
                            .Average(), 2),
                    ReviewCount = g.Count()
                })
                .ToList();
        }

        private string GenerateMainInsight(CourseAnalyticsDto dto)
            {
                if (dto.NegativePercent > 40)
                    return "Nivelul de feedback negativ este ridicat și necesită intervenție.";

                if (dto.CourseScore >= 4)
                    return "Cursul are performanță bună și feedback favorabil.";

                if (dto.CourseScore > 0 && dto.CourseScore < 3)
                    return "Scorul general al cursului este scăzut și necesită îmbunătățiri.";

                if (dto.TopProblems.Any())
                    return $"Tema dominantă în feedback este: {dto.TopProblems.First().Name}.";

                return "Nu există încă suficiente date pentru un insight clar.";
            }

        private List<TopicSummaryDto> ExtractStudentTopTopics(List<Data.Entities.StudentEvaluation> evaluations)
            {
                var topicCounts = new Dictionary<string, int>();

                foreach (var json in evaluations
                    .Select(x => x.TopicsJson)
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    try
                    {
                        var topics = Newtonsoft.Json.Linq.JArray.Parse(json!);

                        foreach (var topic in topics)
                        {
                            var name = topic["name"]?.ToString();

                            if (string.IsNullOrWhiteSpace(name))
                                continue;

                            if (!topicCounts.ContainsKey(name))
                                topicCounts[name] = 0;

                            topicCounts[name]++;
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
                    .Take(5)
                    .Select(x => new TopicSummaryDto
                    {
                        Name = x.Key,
                        Count = x.Value,
                        Percent = Math.Round((double)x.Value / total * 100, 2)
                    })
                    .ToList();
            }

        private List<TrendDto> CalculateStudentTrend(List<Data.Entities.StudentEvaluation> evaluations)
            {
                return evaluations
                    .GroupBy(x => new
                    {
                        x.CreatedAt.Year,
                        x.CreatedAt.Month
                    })
                    .OrderBy(x => x.Key.Year)
                    .ThenBy(x => x.Key.Month)
                    .Select(g => new TrendDto
                    {
                        Month = $"{g.Key.Month:00}.{g.Key.Year}",
                        AverageRating = Math.Round(g.Average(x => x.Rating), 2),
                        PositivePercent = Math.Round(
                            g.Where(x => x.PositivePercent.HasValue)
                                .Select(x => x.PositivePercent!.Value)
                                .DefaultIfEmpty(0)
                                .Average(), 2),
                        NegativePercent = Math.Round(
                            g.Where(x => x.NegativePercent.HasValue)
                                .Select(x => x.NegativePercent!.Value)
                                .DefaultIfEmpty(0)
                                .Average(), 2),
                        ReviewCount = g.Count()
                    })
                    .ToList();
            }

        private List<string> GenerateStudentAlerts(StudentAnalyticsDto dto)
            {
                var alerts = new List<string>();

                if (dto.AverageRiskScore >= 70)
                    alerts.Add("Cursantul are risc ridicat de abandon.");

                if (dto.AverageProgressScore > 0 && dto.AverageProgressScore < 3)
                    alerts.Add("Progresul cursantului este scăzut.");

                if (dto.AverageAttendanceScore > 0 && dto.AverageAttendanceScore < 3)
                    alerts.Add("Prezența cursantului este problematică.");

                if (dto.NegativePercent > 40)
                    alerts.Add("Evaluările profesorilor indică sentiment negativ ridicat.");

                return alerts;
            }

        private List<string> GenerateStudentRecommendations(StudentAnalyticsDto dto)
            {
                var recommendations = new List<string>();

                if (dto.AverageRiskScore >= 70)
                    recommendations.Add("Recomandare: contactează cursantul și stabilește o discuție individuală.");

                if (dto.AverageProgressScore > 0 && dto.AverageProgressScore < 3)
                    recommendations.Add("Recomandare: oferă exerciții suplimentare și sesiuni de recapitulare.");

                if (dto.AverageAttendanceScore > 0 && dto.AverageAttendanceScore < 3)
                    recommendations.Add("Recomandare: verifică motivele absențelor și discută cu cursantul.");

                if (dto.AverageBehaviorScore > 0 && dto.AverageBehaviorScore < 3)
                    recommendations.Add("Recomandare: monitorizează comportamentul cursantului la următoarele sesiuni.");

                if (!recommendations.Any())
                    recommendations.Add("Cursantul are o evoluție stabilă. Continuă monitorizarea periodică.");

                return recommendations;
            }

        private string GenerateStudentSummary(StudentAnalyticsDto dto)
            {
                if (dto.AverageRiskScore >= 70)
                    return $"Cursantul prezintă risc ridicat, cu un scor AI mediu de {dto.AverageRiskScore}% și progres mediu {dto.AverageProgressScore}/5.";

                if (dto.AverageProgressScore >= 4 && dto.AverageAttendanceScore >= 4)
                    return $"Cursantul are o evoluție bună, cu prezență și progres peste medie.";

                if (dto.NegativePercent > dto.PositivePercent)
                    return "Evaluările indică dificultăți recurente care necesită monitorizare.";

                return "Cursantul are o evoluție generală stabilă.";
            } 

        private string GenerateStudentMainInsight(StudentAnalyticsDto dto)
            {
                if (dto.AverageRiskScore >= 70)
                    return "Risc ridicat de abandon.";

                if (dto.AverageProgressScore < 3 && dto.AverageProgressScore > 0)
                    return "Progres scăzut.";

                if (dto.AverageAttendanceScore < 3 && dto.AverageAttendanceScore > 0)
                    return "Prezență scăzută.";

                return "Evoluție stabilă.";
            }
        }
    }

