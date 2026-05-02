using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Feedback.Analytics.Models;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace ERPSystem.Modules.Feedback.Analytics
{
    public class StudentAnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public StudentAnalyticsService(ApplicationDbContext context)
        {
            _context = context;
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

            var percentages = NormalizePercentages(
                evaluations.Where(x => x.PositivePercent.HasValue).Select(x => x.PositivePercent!.Value),
                evaluations.Where(x => x.NegativePercent.HasValue).Select(x => x.NegativePercent!.Value),
                evaluations.Where(x => x.NeutralPercent.HasValue).Select(x => x.NeutralPercent!.Value)
            );

            var dto = new StudentAnalyticsDto
            {
                StudentId = studentId,

                TotalEvaluations = evaluations.Count,
                LastEvaluationDate = evaluations.Max(x => x.CreatedAt),

                AverageRating = AverageOrZero(evaluations.Select(x => x.Rating)),
                AverageAttendanceScore = AverageOrZero(evaluations.Where(x => x.AttendanceScore.HasValue).Select(x => x.AttendanceScore!.Value)),
                AverageBehaviorScore = AverageOrZero(evaluations.Where(x => x.BehaviorScore.HasValue).Select(x => x.BehaviorScore!.Value)),
                AverageProgressScore = AverageOrZero(evaluations.Where(x => x.ProgressScore.HasValue).Select(x => x.ProgressScore!.Value)),
                AverageRiskScore = AverageOrZero(evaluations.Where(x => x.StudentRiskScore.HasValue).Select(x => x.StudentRiskScore!.Value)),

                BehaviorScoreNlp = AverageOrZero(evaluations.Where(x => x.BehaviorScoreNlp.HasValue).Select(x => x.BehaviorScoreNlp!.Value)),
                ProgressScoreNlp = AverageOrZero(evaluations.Where(x => x.ProgressScoreNlp.HasValue).Select(x => x.ProgressScoreNlp!.Value)),

                PositivePercent = percentages.Positive,
                NegativePercent = percentages.Negative,
                NeutralPercent = percentages.Neutral,

                PositiveEvaluationsCount = evaluations.Count(x => x.Sentiment == "pozitiv"),
                NegativeEvaluationsCount = evaluations.Count(x => x.Sentiment == "negativ"),
                NeutralEvaluationsCount = evaluations.Count(x => x.Sentiment == "neutru"),

                TopProblems = ExtractStudentTopTopics(evaluations),
                Trend = CalculateStudentTrend(evaluations)
            };

            dto.NeedsIntervention =
                dto.AverageRiskScore >= 70 ||
                dto.NegativePercent >= 50 ||
                dto.AverageProgressScore < 3 ||
                dto.AverageAttendanceScore < 3;
              
            dto.RiskLevel = dto.AverageRiskScore switch
            {
                >= 70 => "high",
                >= 40 => "medium",
                _ => "low"
            };

            dto.Alerts = GenerateStudentAlerts(dto);

            if (dto.NeedsIntervention)
                dto.Alerts.Insert(0, "Cursantul necesită intervenție sau monitorizare atentă.");

            dto.Recommendations = GenerateStudentRecommendations(dto);
            dto.Summary = GenerateStudentSummary(dto);
            dto.MainInsight = GenerateStudentMainInsight(dto);

            return response.SetSuccess(dto);
        }

        private List<TopicSummaryDto> ExtractStudentTopTopics(List<StudentEvaluation> evaluations)
        {
            var topicCounts = new Dictionary<string, int>();

            foreach (var json in evaluations.Select(x => x.TopicsJson).Where(x => !string.IsNullOrWhiteSpace(x)))
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
                .Take(5)
                .Select(x => new TopicSummaryDto
                {
                    Name = x.Key,
                    Count = x.Value,
                    Percent = Math.Round((double)x.Value / total * 100, 2)
                })
                .ToList();
        }

        private List<TrendDto> CalculateStudentTrend(List<StudentEvaluation> evaluations)
        {
            return evaluations
                .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month })
                .OrderBy(x => x.Key.Year)
                .ThenBy(x => x.Key.Month)
                .Select(g => new TrendDto
                {
                    Month = $"{g.Key.Month:00}.{g.Key.Year}",
                    AverageRating = Math.Round(g.Average(x => x.Rating), 2),
                    PositivePercent = AverageOrZero(g.Where(x => x.PositivePercent.HasValue).Select(x => x.PositivePercent!.Value)),
                    NegativePercent = AverageOrZero(g.Where(x => x.NegativePercent.HasValue).Select(x => x.NegativePercent!.Value)),
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
                return "Cursantul are o evoluție bună, cu prezență și progres peste medie.";

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

        private static (double Positive, double Negative, double Neutral) NormalizePercentages(  IEnumerable<int> positives,  IEnumerable<int> negatives, IEnumerable<int> neutrals)
        {
            var positive = positives.Any() ? positives.Average() : 0;
            var negative = negatives.Any() ? negatives.Average() : 0;
            var neutral = neutrals.Any() ? neutrals.Average() : 0;

            var total = positive + negative + neutral;

            if (total == 0)
                return (0, 0, 0);

            var normalizedPositive = Math.Round(positive / total * 100, 2);
            var normalizedNegative = Math.Round(negative / total * 100, 2);
            var normalizedNeutral = Math.Round(100 - normalizedPositive - normalizedNegative, 2);

            return (normalizedPositive, normalizedNegative, normalizedNeutral);
        }
    }
}