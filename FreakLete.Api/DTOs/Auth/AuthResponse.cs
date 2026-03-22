namespace FreakLete.Api.DTOs.Auth;

public class AuthResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
