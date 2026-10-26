using System.Security.Claims;

namespace QuizMaster.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier);

    // For endpoints that require sign-in, where a missing id would be a bug.
    public static string RequireUserId(this ClaimsPrincipal user) =>
        user.GetUserId() ?? throw new InvalidOperationException("The request has no signed-in user.");
}
