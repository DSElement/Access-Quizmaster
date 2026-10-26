using System.Security.Claims;

namespace QuizMaster.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string RequireUserId(this ClaimsPrincipal user) =>
        user.GetUserId() ?? throw new InvalidOperationException("The request has no signed-in user.");
}
