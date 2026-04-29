namespace FreakLete.Api.Services.Rag;

public interface IContextBuilder
{
    Task<FreakAiContext?> BuildAsync(
        int userId,
        string intent,
        string userMessage,
        CancellationToken ct = default);
}
