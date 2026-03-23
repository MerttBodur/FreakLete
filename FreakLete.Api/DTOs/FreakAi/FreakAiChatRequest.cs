using System.ComponentModel.DataAnnotations;
using FreakLete.Api.Services;

namespace FreakLete.Api.DTOs.FreakAi;

public class FreakAiChatRequest
{
    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public List<ChatMessage>? History { get; set; }
}

public class FreakAiChatResponse
{
    public string Reply { get; set; } = string.Empty;
}
