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

        var request = new
        {
            text = text,
            reviewType = reviewType
        };

        var response = await _httpClient.PostAsJsonAsync("/analyze-review", request);

        var raw = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"NLP error: {response.StatusCode} - {raw}");

        var result = await response.Content.ReadFromJsonAsync<AnalyzeReviewResponse>();

        if (result == null)
            throw new Exception($"NLP deserialize failed. Raw response: {raw}");

        return result;
    }
}