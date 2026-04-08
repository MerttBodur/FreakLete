using System.ComponentModel.DataAnnotations;
using FreakLete.Api.Services;

namespace FreakLete.Api.DTOs.FreakAi;

public class FreakAiChatRequest
{
    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public List<ChatMessage>? History { get; set; }

    /// <summary>
    /// Optional intent hint from client: program_generate, program_view,
    /// program_analyze, nutrition_guidance, general_chat.
    /// Falls back to server-side classification if omitted.
    /// </summary>
    [MaxLength(30)]
    public string? Intent { get; set; }
}

public class FreakAiChatResponse
{
    public string Reply { get; set; } = string.Empty;
}
