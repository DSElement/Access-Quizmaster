using System.Security.Claims;
using QuizMaster.Api.Models;

namespace QuizMaster.Api.Auth;

public static class QuizAccess
{
    public static IQueryable<Quiz> VisibleTo(this IQueryable<Quiz> quizzes, string? userId) =>
        quizzes.Where(q =>
            q.Visibility == QuizVisibility.Official ||
            q.Visibility == QuizVisibility.Public ||
            (userId != null && q.OwnerId == userId));

    public static bool CanEditQuiz(this ClaimsPrincipal user, string? ownerId, QuizVisibility visibility) =>
        IsOwner(user, ownerId) ||
        (visibility == QuizVisibility.Official && user.IsInRole(Roles.Admin));

    public static bool CanDeleteQuiz(this ClaimsPrincipal user, string? ownerId, QuizVisibility visibility) =>
        user.CanEditQuiz(ownerId, visibility) ||
        (visibility == QuizVisibility.Public && user.IsInRole(Roles.Admin));

    public static bool CanPublishQuiz(this ClaimsPrincipal user, string? ownerId) =>
        IsOwner(user, ownerId);

    private static bool IsOwner(ClaimsPrincipal user, string? ownerId) =>
        ownerId is not null && ownerId == user.GetUserId();
}
