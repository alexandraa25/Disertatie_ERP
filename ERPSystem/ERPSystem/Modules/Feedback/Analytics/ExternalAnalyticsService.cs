using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using ERPSystem.Modules.Feedback.Analytics.Models;
using ERPSystem.Utils.Response;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace ERPSystem.Modules.Feedback.Analytics
{
    public class ExternalAnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public ExternalAnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PublicResponse> GetExternalAnalyticsAsync(string? targetType = null, string? targetId = null, string? source = null)
        {
            PublicResponse response = new(true);

            var query = _context.ExternalReviews.AsQueryable();

            if (!string.IsNullOrWhiteSpace(targetType))
                query = query.Where(x => x.TargetType == targetType);

            if (!string.IsNullOrWhiteSpace(targetId))
                query = query.Where(x => x.TargetId == targetId);

            if (!string.IsNullOrWhiteSpace(source))
                query = query.Where(x => x.Source == source);

            var reviews = await query
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();

            if (!reviews.Any())
                return response.SetError(
                    "NoExternalReviews",
                    "Nu există feedback extern pentru filtrele selectate."
                );

            var percentages = NormalizePercentages(
                reviews.Where(x => x.PositivePercent.HasValue).Select(x => x.PositivePercent!.Value),
                reviews.Where(x => x.NegativePercent.HasValue).Select(x => x.NegativePercent!.Value),
                reviews.Where(x => x.NeutralPercent.HasValue).Select(x => x.NeutralPercent!.Value)
            );

            var dto = new ExternalAnalyticsDto
            {
                TotalReviews = reviews.Count,

                PositiveReviewsCount = reviews.Count(x => x.Sentiment == "pozitiv"),
                NegativeReviewsCount = reviews.Count(x => x.Sentiment == "negativ"),
                NeutralReviewsCount = reviews.Count(x => x.Sentiment == "neutru"),

                LastReviewDate = reviews.Max(x => x.CreatedAt),

                ReviewsBySource = reviews
                    .Where(x => !string.IsNullOrWhiteSpace(x.Source))
                    .GroupBy(x => x.Source)
                    .ToDictionary(x => x.Key, x => x.Count()),

                ReviewsByTargetType = reviews
                    .Where(x => !string.IsNullOrWhiteSpace(x.TargetType))
                    .GroupBy(x => x.TargetType)
                    .ToDictionary(x => x.Key, x => x.Count()),

                AverageRating = AverageOrZero(
                    reviews
                        .Where(x => x.Rating.HasValue)
                        .Select(x => x.Rating!.Value)
                ),

                PublicPerceptionScore = AverageOrZero(
                    reviews
                        .Where(x => x.PublicPerceptionScore.HasValue)
                        .Select(x => x.PublicPerceptionScore!.Value)
                ),

                PositivePercent = percentages.Positive,
                NegativePercent = percentages.Negative,
                NeutralPercent = percentages.Neutral,

                TopTopics = ExtractTopTopics(reviews),
                Trend = CalculateTrend(reviews)
            };

            dto.UrgentResponseNeeded =
                dto.NegativePercent >= 40 ||
                dto.PublicPerceptionScore < 0.5 ||
                dto.AverageRating < 3;

            dto.ReputationRiskLevel = dto.NegativePercent switch
            {
                >= 50 => "high",
                >= 25 => "medium",
                _ => "low"
            };

            dto.Alerts = GenerateAlerts(dto);
            dto.Recommendations = GenerateRecommendations(dto.TopTopics);
            dto.Summary = GenerateSummary(dto);
            dto.MainInsight = GenerateMainInsight(dto);

            return response.SetSuccess(dto);
        }

        private List<TopicSummaryDto> ExtractTopTopics(List<ExternalReview> reviews)
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

        private List<TrendDto> CalculateTrend(List<ExternalReview> reviews)
        {
            return reviews
                .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month })
                .OrderBy(x => x.Key.Year)
                .ThenBy(x => x.Key.Month)
                .Select(g => new TrendDto
                {
                    Month = $"{g.Key.Month:00}.{g.Key.Year}",
                    AverageRating = Math.Round(
                        g.Where(x => x.Rating.HasValue)
                            .Select(x => x.Rating!.Value)
                            .DefaultIfEmpty(0)
                            .Average(), 2),
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

        private List<string> GenerateAlerts(ExternalAnalyticsDto dto)
        {
            var alerts = new List<string>();

            if (dto.NegativePercent > 40)
                alerts.Add("Feedbackul extern negativ depășește 40%.");

            if (dto.AverageRating > 0 && dto.AverageRating < 3)
                alerts.Add("Ratingul extern mediu este sub 3.");

            if (dto.PublicPerceptionScore > 0 && dto.PublicPerceptionScore < 0.5)
                alerts.Add("Percepția publică este scăzută.");

            return alerts;
        }

        private List<string> GenerateRecommendations(List<TopicSummaryDto> topics)
        {
            var recommendations = new List<string>();

            if (topics.Any(x => x.Name == "Materiale"))
                recommendations.Add("Îmbunătățește materialele prezentate public și resursele oferite cursanților.");

            if (topics.Any(x => x.Name == "Ritm curs"))
                recommendations.Add("Clarifică așteptările privind ritmul cursului în descrierea publică.");

            if (topics.Any(x => x.Name == "Calitate profesor"))
                recommendations.Add("Evidențiază mai bine experiența profesorilor și modul de suport oferit.");

            if (!recommendations.Any())
                recommendations.Add("Menține monitorizarea feedbackului extern pentru evoluția reputației.");

            return recommendations;
        }

        private string GenerateSummary(ExternalAnalyticsDto dto)
        {
            var mainTopic = dto.TopTopics.FirstOrDefault()?.Name;

            if (dto.NegativePercent > 40 && mainTopic != null)
                return $"Percepția externă indică nemulțumiri recurente. Tema principală menționată este '{mainTopic}'.";

            if (dto.PositivePercent > dto.NegativePercent)
                return $"Percepția externă este predominant pozitivă, cu {dto.PositivePercent}% feedback pozitiv.";

            if (dto.NegativePercent > dto.PositivePercent)
                return "Percepția externă este mai degrabă negativă și necesită analiză suplimentară.";

            return "Feedbackul extern este mixt.";
        }

        private string GenerateMainInsight(ExternalAnalyticsDto dto)
        {
            if (dto.PublicPerceptionScore >= 0.7)
                return "Reputația externă este bună.";

            if (dto.PublicPerceptionScore > 0 && dto.PublicPerceptionScore < 0.5)
                return "Reputația externă necesită îmbunătățiri.";

            if (dto.NegativePercent > 40)
                return "Feedbackul extern negativ este ridicat.";

            return "Nu există încă un semnal extern critic.";
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


        private static (double Positive, double Negative, double Neutral) NormalizePercentages( IEnumerable<int> positives, IEnumerable<int> negatives,  IEnumerable<int> neutrals)
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