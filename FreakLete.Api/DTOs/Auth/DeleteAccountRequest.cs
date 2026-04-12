using System.ComponentModel.DataAnnotations;

namespace FreakLete.Api.DTOs.Auth;

public class DeleteAccountRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;
}
