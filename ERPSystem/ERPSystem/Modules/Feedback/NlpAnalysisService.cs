using ERPSystem.Modules.Feedback.Models;
using System.Net.Http.Json;

public interface INlpAnalysisService
{
    Task<AnalyzeReviewResponse?> AnalyzeAsync(string text, string reviewType);
}

public class NlpAnalysisService : INlpAnalysisService
{
    private readonly HttpClient _httpClient;

    public NlpAnalysisService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AnalyzeReviewResponse?> AnalyzeAsync(string text, string reviewType)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            var request = new AnalyzeReviewRequest
            {
                Text = text,
                ReviewType = reviewType
            };

            var response = await _httpClient.PostAsJsonAsync("/analyze-review", request);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<AnalyzeReviewResponse>();
        }
        catch
        {
            return null;
        }
    }
}