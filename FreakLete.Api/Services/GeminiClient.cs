using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FreakLete.Api.Services;

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.5-flash-lite";
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

    public async Task<GeminiResponse> GenerateContentAsync(GeminiRequest request)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        var json = JsonSerializer.Serialize(request, JsonOpts);
        _logger.LogDebug("Gemini request: {Json}", json);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API error {Status}: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Gemini API error: {response.StatusCode} — {responseBody}");
        }

        _logger.LogDebug("Gemini response: {Body}", responseBody);

        return JsonSerializer.Deserialize<GeminiResponse>(responseBody, JsonOpts)
            ?? throw new InvalidOperationException("Failed to deserialize Gemini response");
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
