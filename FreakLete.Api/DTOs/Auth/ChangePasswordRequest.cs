using System.ComponentModel.DataAnnotations;

namespace FreakLete.Api.DTOs.Auth;

public class ChangePasswordRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    public string NewPasswordRepeat { get; set; } = string.Empty;
}
