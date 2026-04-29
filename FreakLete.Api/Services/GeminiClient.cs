using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FreakLete.Api.Services;

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.5-flash-lite";
    public string EmbeddingModel { get; set; } = "text-embedding-004";
}

public class GeminiClient
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public GeminiClient(HttpClient http, GeminiOptions options, ILogger<GeminiClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<GeminiResponse> GenerateContentAsync(GeminiRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        var json = JsonSerializer.Serialize(request, JsonOpts);
        _logger.LogDebug("Gemini request sent to model {Model}", _options.Model);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API error {Status} from model {Model}", response.StatusCode, _options.Model);
            throw new InvalidOperationException($"Gemini API error: {response.StatusCode}");
        }

        _logger.LogDebug("Gemini response received from model {Model}", _options.Model);

        return JsonSerializer.Deserialize<GeminiResponse>(responseBody, JsonOpts)
            ?? throw new InvalidOperationException("Failed to deserialize Gemini response");
    }

    public async Task<float[]?> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Gemini embedding skipped: empty text payload");
            return null;
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.EmbeddingModel}:embedContent?key={_options.ApiKey}";
        var request = new GeminiEmbeddingRequest
        {
            Content = new GeminiEmbeddingContent
            {
                Parts = [new GeminiEmbeddingPart { Text = text }]
            }
        };

        var json = JsonSerializer.Serialize(request, JsonOpts);

        try
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini embed API error {Status} from model {Model}", response.StatusCode, _options.EmbeddingModel);
                return null;
            }

            var payload = JsonSerializer.Deserialize<GeminiEmbeddingResponse>(responseBody, JsonOpts);
            var values = payload?.Embedding?.Values;

            if (values is null || values.Length == 0)
            {
                _logger.LogWarning("Gemini embed response malformed or empty for model {Model}", _options.EmbeddingModel);
                return null;
            }

            return values;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini embed request failed for model {Model}", _options.EmbeddingModel);
            return null;
        }
    }
}

// ── Gemini API request/response models ──────────────────────────────

public class GeminiRequest
{
    public GeminiSystemInstruction? SystemInstruction { get; set; }
    public List<GeminiContent> Contents { get; set; } = [];
    public List<GeminiTool>? Tools { get; set; }
    public GeminiGenerationConfig? GenerationConfig { get; set; }
}

public class GeminiSystemInstruction
{
    public List<GeminiPart> Parts { get; set; } = [];
}

public class GeminiContent
{
    public string Role { get; set; } = "user";
    public List<GeminiPart> Parts { get; set; } = [];
}

public class GeminiPart
{
    public string? Text { get; set; }
    public GeminiFunctionCall? FunctionCall { get; set; }
    public GeminiFunctionResponse? FunctionResponse { get; set; }
}

public class GeminiFunctionCall
{
    public string Name { get; set; } = string.Empty;
    public JsonElement? Args { get; set; }
}

public class GeminiFunctionResponse
{
    public string Name { get; set; } = string.Empty;
    public JsonElement Response { get; set; }
}

public class GeminiTool
{
    public List<GeminiFunctionDeclaration> FunctionDeclarations { get; set; } = [];
}

public class GeminiFunctionDeclaration
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GeminiSchema? Parameters { get; set; }
}

public class GeminiSchema
{
    public string Type { get; set; } = "object";
    public Dictionary<string, GeminiSchemaProperty>? Properties { get; set; }
    public List<string>? Required { get; set; }
}

public class GeminiSchemaProperty
{
    public string Type { get; set; } = "string";
    public string? Description { get; set; }
    public GeminiSchemaProperty? Items { get; set; }
    public Dictionary<string, GeminiSchemaProperty>? Properties { get; set; }
    public List<string>? Required { get; set; }
}

public class GeminiGenerationConfig
{
    public double? Temperature { get; set; }
    public int? MaxOutputTokens { get; set; }
}

public class GeminiResponse
{
    public List<GeminiCandidate>? Candidates { get; set; }
}

public class GeminiCandidate
{
    public GeminiContent? Content { get; set; }
    public string? FinishReason { get; set; }
}

public class GeminiEmbeddingRequest
{
    public GeminiEmbeddingContent Content { get; set; } = new();
}

public class GeminiEmbeddingContent
{
    public List<GeminiEmbeddingPart> Parts { get; set; } = [];
}

public class GeminiEmbeddingPart
{
    public string Text { get; set; } = string.Empty;
}

public class GeminiEmbeddingResponse
{
    public GeminiEmbeddingVector? Embedding { get; set; }
}

public class GeminiEmbeddingVector
{
    public float[]? Values { get; set; }
}
