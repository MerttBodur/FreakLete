namespace FreakLete.Api.Entities;

public class AuthLoginAttempt
{
    public int Id { get; set; }
    public string NormalizedEmail { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public bool WasSuccessful { get; set; }
}
