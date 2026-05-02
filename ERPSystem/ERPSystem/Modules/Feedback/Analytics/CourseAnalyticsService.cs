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
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            if (!courseReviews.Any())
                return response.SetError("NoCourseReviews", "Nu există recenzii pentru această sesiune.");

            var percentages = NormalizePercentages(
                courseReviews.Where(x => x.PositivePercent.HasValue).Select(x => x.PositivePercent!.Value),
                courseReviews.Where(x => x.NegativePercent.HasValue).Select(x => x.NegativePercent!.Value),
                courseReviews.Where(x => x.NeutralPercent.HasValue).Select(x => x.NeutralPercent!.Value)
            );

            var topProblems = ExtractTopTopics(courseReviews);

            var dto = new CourseAnalyticsDto
            {
                CourseSessionId = courseSessionId,
                TotalReviews = courseReviews.Count,

                AverageRating = AverageOrZero(courseReviews.Select(x => x.Rating)),
                CourseScore = CalculateCourseScore(courseReviews),

                TeacherScore = AverageOrZero(
                    courseReviews
                        .Where(x => x.TeacherScore.HasValue)
                        .Select(x => x.TeacherScore!.Value)),

                BehaviorScore = AverageOrZero(
                    courseReviews
                        .Where(x => x.BehaviorScore.HasValue)
                        .Select(x => x.BehaviorScore!.Value)),

                PositivePercent = percentages.Positive,
                NegativePercent = percentages.Negative,
                NeutralPercent = percentages.Neutral,

                PositiveReviewsCount = courseReviews.Count(x => x.Sentiment == "pozitiv"),
                NegativeReviewsCount = courseReviews.Count(x => x.Sentiment == "negativ"),
                NeutralReviewsCount = courseReviews.Count(x => x.Sentiment == "neutru"),

                TopProblems = topProblems,
                Trend = CalculateTrend(courseReviews)
            };

            dto.NeedsAttention =
                dto.NegativePercent >= 40 ||
                dto.AverageRating < 3 ||
                dto.CourseScore < 0.6 ||
                dto.TeacherScore < 0.6;

            dto.RiskLevel = dto.NeedsAttention switch
            {
                true when dto.NegativePercent >= 50 || dto.AverageRating < 2.5 => "high",
                true => "medium",
                _ => "low"
            };

            dto.Alerts = GenerateAlerts(dto);
            dto.Recommendations = GenerateRecommendations(dto.TopProblems);
            dto.Summary = GenerateSummary(dto);
            dto.MainInsight = GenerateMainInsight(dto);

            return response.SetSuccess(dto);
        }

        private double CalculateCourseScore(List<CourseReview> reviews)
        {
            var ratingScore = AverageOrZero(reviews.Select(x => x.Rating)) / 5.0;

            var nlpCourseScore = AverageOrZero(
                reviews
                    .Where(x => x.CourseScore.HasValue)
                    .Select(x => x.CourseScore!.Value));

            if (nlpCourseScore == 0)
                nlpCourseScore = ratingScore;

            var detailedScores = new List<double>();

            var structure = AverageOrZero(
                reviews.Where(x => x.CourseStructureRating.HasValue)
                    .Select(x => x.CourseStructureRating!.Value)) / 5.0;

            var pace = AverageOrZero(
                reviews.Where(x => x.CoursePaceRating.HasValue)
                    .Select(x => x.CoursePaceRating!.Value)) / 5.0;

            var materials = AverageOrZero(
                reviews.Where(x => x.MaterialsRating.HasValue)
                    .Select(x => x.MaterialsRating!.Value)) / 5.0;

            if (structure > 0) detailedScores.Add(structure);
            if (pace > 0) detailedScores.Add(pace);
            if (materials > 0) detailedScores.Add(materials);

            var detailedScore = detailedScores.Any()
                ? detailedScores.Average()
                : ratingScore;

            return Math.Round(
                ratingScore * 0.4 +
                detailedScore * 0.3 +
                nlpCourseScore * 0.3,
                2
            );
        }

        private List<TopicSummaryDto> ExtractTopTopics(List<CourseReview> reviews)
        {
            var topicCounts = new Dictionary<string, int>();

            foreach (var json in reviews.Select(x => x.TopicsJson).Where(x => !string.IsNullOrWhiteSpace(x)))
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

        private List<TrendDto> CalculateTrend(List<CourseReview> reviews)
        {
            return reviews
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

        private List<string> GenerateAlerts(CourseAnalyticsDto dto)
        {
            var alerts = new List<string>();

            if (dto.NegativePercent >= 40)
                alerts.Add("Sentimentul negativ depășește 40%.");

            if (dto.AverageRating > 0 && dto.AverageRating < 3)
                alerts.Add("Ratingul mediu este sub 3.");

            if (dto.CourseScore > 0 && dto.CourseScore < 0.6)
                alerts.Add("Scorul general al cursului este scăzut.");

            if (dto.TeacherScore > 0 && dto.TeacherScore < 0.6)
                alerts.Add("Scorul profesorului este sub nivelul recomandat.");

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

            if (topics.Any(x => x.Name == "Structură curs"))
                recommendations.Add("Revizuiește structura cursului și ordinea capitolelor.");

            if (!recommendations.Any())
                recommendations.Add("Cursul are feedback stabil. Continuă monitorizarea recenziilor.");

            return recommendations;
        }

        private string GenerateSummary(CourseAnalyticsDto dto)
        {
            var mainProblem = dto.TopProblems.FirstOrDefault()?.Name;

            if (dto.NegativePercent >= 40 && mainProblem != null)
                return $"Feedback-ul indică nemulțumire ridicată. Principala problemă este '{mainProblem}', iar ratingul mediu este {dto.AverageRating}/5.";

            if (dto.PositivePercent >= 60)
                return $"Cursul are o percepție pozitivă, cu {dto.PositivePercent}% sentiment pozitiv și rating mediu {dto.AverageRating}/5.";

            if (dto.NegativePercent > dto.PositivePercent)
                return "Feedback-ul sugerează nemulțumiri recurente și necesită analiză detaliată.";

            return $"Feedback-ul este mixt. Ratingul mediu este {dto.AverageRating}/5.";
        }

        private string GenerateMainInsight(CourseAnalyticsDto dto)
        {
            if (dto.NegativePercent >= 40)
                return "Nivelul de feedback negativ este ridicat și necesită intervenție.";

            if (dto.CourseScore >= 0.8)
                return "Cursul are performanță bună și feedback favorabil.";

            if (dto.CourseScore > 0 && dto.CourseScore < 0.6)
                return "Scorul general al cursului este scăzut și necesită îmbunătățiri.";

            if (dto.TopProblems.Any())
                return $"Tema dominantă în feedback este: {dto.TopProblems.First().Name}.";

            return "Nu există încă suficiente date pentru un insight clar.";
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

        private static (double Positive, double Negative, double Neutral) NormalizePercentages(
            IEnumerable<int> positives,
            IEnumerable<int> negatives,
            IEnumerable<int> neutrals)
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