using System.Net.Http.Json;
using System.Text.Json;

namespace Momentix.API.Services;

public class ChallengeVisionService
{
    private const int DefaultMinimumConfidence = 80;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChallengeVisionService> _logger;

    public ChallengeVisionService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ChallengeVisionService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ChallengeVisionEvaluation> EvaluateAsync(
        string challengeDescription,
        string imagePath,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var model = _configuration["Ollama:Model"] ?? "llava";
        var baseUrl = _configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var timeoutSeconds = Math.Clamp(_configuration.GetValue("Ollama:TimeoutSeconds", 90), 5, 300);
        var minimumConfidence = Math.Clamp(_configuration.GetValue("Ollama:MinimumConfidence", DefaultMinimumConfidence), 1, 100);

        try
        {
            if (!File.Exists(imagePath))
                return Unavailable(model, "AI check unavailable: uploaded image file was not found.");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var imageBase64 = Convert.ToBase64String(await File.ReadAllBytesAsync(imagePath, timeoutCts.Token));
            var endpoint = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "api/chat");
            var prompt = BuildPrompt(challengeDescription, minimumConfidence);

            var request = new
            {
                model,
                stream = false,
                format = "json",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt,
                        images = new[] { imageBase64 }
                    }
                }
            };

            using var response = await _httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                _logger.LogWarning("Ollama returned {StatusCode} while checking challenge image: {Body}", response.StatusCode, body);
                return Unavailable(model, "AI check unavailable. Start Ollama and use a vision model.");
            }

            var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions, timeoutCts.Token);
            var content = ollamaResponse?.Message?.Content ?? ollamaResponse?.Response;
            if (string.IsNullOrWhiteSpace(content))
                return Unavailable(model, "AI check unavailable: the model returned an empty response.");

            var decision = ParseDecision(content);
            if (decision == null)
                return Unavailable(model, "AI check unavailable: the model response could not be read.");

            var confidence = Math.Clamp(decision.Confidence, 0, 100);
            var isNonCameraImage = IsNonCameraImage(decision.ImageType);
            var isSatisfied = decision.IsSatisfied && confidence >= minimumConfidence && !isNonCameraImage;
            var feedback = BuildFeedback(decision, confidence, minimumConfidence, isNonCameraImage);

            return new ChallengeVisionEvaluation
            {
                IsSatisfied = isSatisfied,
                Confidence = confidence,
                Feedback = feedback,
                Model = model,
                EvaluatedAt = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Unavailable(model, "AI check timed out. The photo was saved, but Ollama did not answer in time.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama challenge image evaluation failed.");
            return Unavailable(model, "AI check unavailable. Make sure Ollama is running and the selected model supports images.");
        }
    }

    private static string BuildPrompt(string challengeDescription, int minimumConfidence) =>
        "You are a strict judge for a real-world mobile photo challenge. " +
        $"Challenge: \"{challengeDescription}\". " +
        "The upload must look like a real camera photo taken for this challenge. " +
        "Reject screenshots, game scenes, menus, app screens, logos, wallpapers, drawings, AI art, memes, and edited graphics unless the challenge explicitly asks for them. " +
        "The matching thing must be a clear main subject, not only tiny text, a small accent, lighting, or a vague background color. " +
        "For color challenges, use strict color names: purple, violet, lavender, magenta, and indigo are not blue; yellow/green/orange/red/brown/gray are not blue. " +
        "A mostly purple image must fail a blue challenge. A digital blue character in a game screenshot must fail because it is not a real camera photo. " +
        $"Set isSatisfied to true only when you are at least {minimumConfidence}% confident. Otherwise set isSatisfied to false. " +
        "Return only valid JSON with this exact shape: " +
        "{\"isSatisfied\":false,\"confidence\":72,\"imageType\":\"real_photo|screenshot|digital_art|unclear\",\"matchedObject\":\"short object name\",\"observedColor\":\"short color\",\"feedback\":\"short reason\"}. " +
        "confidence must be an integer from 0 to 100.";

    private static string BuildFeedback(
        ChallengeVisionDecision decision,
        int confidence,
        int minimumConfidence,
        bool isNonCameraImage)
    {
        var feedback = string.IsNullOrWhiteSpace(decision.Feedback)
            ? "AI checked the uploaded photo."
            : decision.Feedback.Trim();

        if (isNonCameraImage)
            return $"Not accepted: this looks like {decision.ImageType}, not a real camera photo. {feedback}";

        if (decision.IsSatisfied && confidence < minimumConfidence)
            return $"Not accepted: AI confidence is {confidence}%, below the {minimumConfidence}% test threshold. {feedback}";

        return feedback;
    }

    private static bool IsNonCameraImage(string? imageType)
    {
        if (string.IsNullOrWhiteSpace(imageType))
            return false;

        var normalized = imageType.Trim().ToLowerInvariant();
        return normalized.Contains("screenshot")
            || normalized.Contains("game")
            || normalized.Contains("digital")
            || normalized.Contains("art")
            || normalized.Contains("menu")
            || normalized.Contains("logo")
            || normalized.Contains("wallpaper")
            || normalized.Contains("meme")
            || normalized.Contains("screen");
    }

    private static ChallengeVisionDecision? ParseDecision(string content)
    {
        var json = ExtractJson(content);
        if (json == null)
            return null;

        try
        {
            return JsonSerializer.Deserialize<ChallengeVisionDecision>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractJson(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');

        return start >= 0 && end > start
            ? content[start..(end + 1)]
            : null;
    }

    private static ChallengeVisionEvaluation Unavailable(string model, string feedback) =>
        new()
        {
            IsSatisfied = null,
            Confidence = null,
            Feedback = feedback,
            Model = model,
            EvaluatedAt = DateTime.UtcNow
        };

    private sealed class OllamaChatResponse
    {
        public OllamaMessage? Message { get; set; }
        public string? Response { get; set; }
    }

    private sealed class OllamaMessage
    {
        public string? Content { get; set; }
    }

    private sealed class ChallengeVisionDecision
    {
        public bool IsSatisfied { get; set; }
        public int Confidence { get; set; }
        public string? ImageType { get; set; }
        public string? MatchedObject { get; set; }
        public string? ObservedColor { get; set; }
        public string? Feedback { get; set; }
    }
}

public class ChallengeVisionEvaluation
{
    public bool? IsSatisfied { get; init; }
    public int? Confidence { get; init; }
    public string? Feedback { get; init; }
    public string? Model { get; init; }
    public DateTime? EvaluatedAt { get; init; }
}
