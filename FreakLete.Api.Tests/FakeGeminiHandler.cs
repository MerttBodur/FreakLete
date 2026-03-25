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
    /// Configure the handler to return a candidate with only whitespace text.
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
        return _handler(request, cancellationToken);
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
