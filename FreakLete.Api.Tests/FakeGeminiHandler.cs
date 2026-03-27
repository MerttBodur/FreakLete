using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FreakLete.Api.Services;

namespace FreakLete.Api.Tests;

/// <summary>
/// Fake HttpMessageHandler that intercepts Gemini API calls and returns controlled responses.
/// Used by FreakAI integration tests to avoid real Gemini calls.
/// </summary>
public class FakeGeminiHandler : HttpMessageHandler
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler = null!;
    
    // ── Request capture for prompt verification ──────────────────
    private GeminiRequest? _lastRequest = null;
    
    /// <summary>
    /// Get the most recent GeminiRequest sent (for verifying system prompt content).
    /// </summary>
    public GeminiRequest? GetLastRequest() => _lastRequest;
    
    /// <summary>
    /// Verify that the system prompt contains a specific phrase (case-insensitive).
    /// Returns true if the phrase is found in the system instruction text.
    /// </summary>
    public bool VerifySystemPromptContains(string phrase)
    {
        if (_lastRequest?.SystemInstruction?.Parts == null || _lastRequest.SystemInstruction.Parts.Count == 0)
            return false;
        
        var promptText = _lastRequest.SystemInstruction.Parts[0].Text ?? "";
        return promptText.Contains(phrase, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Assert that the system prompt contains the CORE PRODUCT RULE text.
    /// Throws if the rule is not found.
    /// </summary>
    public void AssertSystemPromptIncludesCoreProductRule()
    {
        if (!VerifySystemPromptContains("CORE PRODUCT RULE"))
            throw new InvalidOperationException("System prompt does not contain 'CORE PRODUCT RULE' text");
        
        if (!VerifySystemPromptContains("Default is to help"))
            throw new InvalidOperationException("System prompt does not contain 'Default is to help' text");
        
        if (!VerifySystemPromptContains("Missing profile data must NEVER block an answer"))
            throw new InvalidOperationException("System prompt does not contain mandatory rule about missing profile data");
    }

    public FakeGeminiHandler()
    {
        // Default: return a simple text response
        SetupTextResponse("Hello from fake Gemini!");
    }

    /// <summary>
    /// Configure the handler to return a simple text response from the model.
    /// </summary>
    public void SetupTextResponse(string text)
    {
        _handler = (_, _) => Task.FromResult(MakeGeminiResponse(text));
    }

    /// <summary>
    /// Configure the handler to return a response with function calls.
    /// After tool results are sent back, returns the final text.
    /// </summary>
    public void SetupToolCallThenText(string toolName, JsonElement? toolArgs, string finalText)
    {
        int callCount = 0;
        _handler = (_, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                // First call: return a function call
                return Task.FromResult(MakeFunctionCallResponse(toolName, toolArgs));
            }
            // Subsequent calls: return text
            return Task.FromResult(MakeGeminiResponse(finalText));
        };
    }

    /// <summary>
    /// Configure the handler to return two different tool calls in sequence, then text.
    /// Round 1: toolName1, Round 2: toolName2, Round 3+: finalText.
    /// </summary>
    public void SetupTwoToolCallsThenText(
        string toolName1, JsonElement? toolArgs1,
        string toolName2, JsonElement? toolArgs2,
        string finalText)
    {
        int callCount = 0;
        _handler = (_, _) =>
        {
            callCount++;
            return Task.FromResult(callCount switch
            {
                1 => MakeFunctionCallResponse(toolName1, toolArgs1),
                2 => MakeFunctionCallResponse(toolName2, toolArgs2),
                _ => MakeGeminiResponse(finalText)
            });
        };
    }

    /// <summary>
    /// Configure the handler to always return function calls (to test max-rounds behavior).
    /// </summary>
    public void SetupInfiniteToolCalls(string toolName)
    {
        _handler = (_, _) => Task.FromResult(MakeFunctionCallResponse(toolName, null));
    }

    /// <summary>
    /// Configure the handler to return an empty candidate (no content).
    /// </summary>
    public void SetupEmptyCandidate()
    {
        _handler = (_, _) =>
        {
            var response = new GeminiResponse
            {
                Candidates = [new GeminiCandidate { Content = null }]
            };
            return Task.FromResult(MakeHttpResponse(HttpStatusCode.OK, response));
        };
    }

    /// <summary>
    /// Configure the handler to return a blank text response.
    /// </summary>
    public void SetupBlankTextResponse()
    {
        _handler = (_, _) =>
        {
            var response = new GeminiResponse
            {
                Candidates =
                [
                    new GeminiCandidate
                    {
                        Content = new GeminiContent
                        {
                            Role = "model",
                            Parts = [new GeminiPart { Text = "   " }]
                        }
                    }
                ]
            };
            return Task.FromResult(MakeHttpResponse(HttpStatusCode.OK, response));
        };
    }

    /// <summary>
    /// Configure handler to call a tool first, then return blank text.
    /// Simulates the confirmation/follow-up failure where second message with history gets no response.
    /// </summary>
    public void SetupToolCallThenBlank(string toolName, JsonElement? toolArgs)
    {
        int callCount = 0;
        _handler = (_, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                return Task.FromResult(MakeFunctionCallResponse(toolName, toolArgs));
            }
            // Second request returns blank — reproduces the confirmation follow-up failure
            var response = new GeminiResponse
            {
                Candidates =
                [
                    new GeminiCandidate
                    {
                        Content = new GeminiContent
                        {
                            Role = "model",
                            Parts = [new GeminiPart { Text = "   " }]
                        }
                    }
                ]
            };
            return Task.FromResult(MakeHttpResponse(HttpStatusCode.OK, response));
        };
    }

    /// <summary>
    /// Configure the handler to return an HTTP error (triggers InvalidOperationException in GeminiClient).
    /// </summary>
    public void SetupHttpError(HttpStatusCode statusCode, string body = "Gemini error")
    {
        _handler = (_, _) =>
        {
            var msg = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body)
            };
            return Task.FromResult(msg);
        };
    }

    /// <summary>
    /// Configure the handler to throw HttpRequestException (network error).
    /// </summary>
    public void SetupNetworkError()
    {
        _handler = (_, _) => throw new HttpRequestException("Simulated network failure");
    }

    /// <summary>
    /// Configure the handler to throw TaskCanceledException (timeout).
    /// </summary>
    public void SetupTimeout()
    {
        _handler = (_, _) => throw new TaskCanceledException("Simulated timeout");
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Capture the request for verification
        CaptureRequest(request);
        return _handler(request, cancellationToken);
    }
    
    private void CaptureRequest(HttpRequestMessage request)
    {
        // Deserialize the request body to capture GeminiRequest
        if (request.Content is not null)
        {
            try
            {
                // For StringContent (which GeminiClient uses), we can get the string synchronously
                string? contentString = null;
                
                if (request.Content is StringContent sc)
                {
                    // StringContent stores content directly, we can retrieve it
                    // Unfortunately, StringContent doesn't expose the original string
                    // So we fall back to reading from the stream
                    contentString = sc.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                else
                {
                    // For other content types, read from stream
                    var stream = request.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                    using (var reader = new System.IO.StreamReader(stream))
                    {
                        contentString = reader.ReadToEnd();
                    }
                }
                
                if (!string.IsNullOrEmpty(contentString))
                {
                    _lastRequest = JsonSerializer.Deserialize<GeminiRequest>(contentString, JsonOpts);
                }
            }
            catch (Exception ex)
            {
                // If deserialization fails, silently continue — this is for verification only
                // We don't want failures in our verification logic to break the tests
                System.Diagnostics.Debug.WriteLine($"Failed to capture request: {ex.Message}");
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static HttpResponseMessage MakeGeminiResponse(string text)
    {
        var response = new GeminiResponse
        {
            Candidates =
            [
                new GeminiCandidate
                {
                    Content = new GeminiContent
                    {
                        Role = "model",
                        Parts = [new GeminiPart { Text = text }]
                    }
                }
            ]
        };
        return MakeHttpResponse(HttpStatusCode.OK, response);
    }

    private static HttpResponseMessage MakeFunctionCallResponse(string toolName, JsonElement? args)
    {
        var response = new GeminiResponse
        {
            Candidates =
            [
                new GeminiCandidate
                {
                    Content = new GeminiContent
                    {
                        Role = "model",
                        Parts =
                        [
                            new GeminiPart
                            {
                                FunctionCall = new GeminiFunctionCall
                                {
                                    Name = toolName,
                                    Args = args
                                }
                            }
                        ]
                    }
                }
            ]
        };
        return MakeHttpResponse(HttpStatusCode.OK, response);
    }

    private static HttpResponseMessage MakeHttpResponse(HttpStatusCode status, GeminiResponse body)
    {
        var json = JsonSerializer.Serialize(body, JsonOpts);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }
}
