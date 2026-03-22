using System.Security.Claims;

namespace FreakLete.Api.Services;

public static class UserExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID claim not found.");
        return int.Parse(claim.Value);
    }
}
