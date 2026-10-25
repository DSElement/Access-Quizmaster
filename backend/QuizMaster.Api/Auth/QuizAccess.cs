using QuizMaster.Api.Models;

namespace QuizMaster.Api.Auth;

public static class QuizAccess
{
    public static IQueryable<Quiz> VisibleTo(this IQueryable<Quiz> quizzes, string? userId) =>
        quizzes.Where(q =>
            q.Visibility == QuizVisibility.Official ||
            q.Visibility == QuizVisibility.Public ||
            (userId != null && q.OwnerId == userId));
}
