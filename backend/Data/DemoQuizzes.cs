using Microsoft.EntityFrameworkCore;
using QuizMaster.Models;

namespace QuizMaster.Data;

internal static class DemoQuizzes
{
    private const string Owner = "00000000-0000-0000-0000-00000000feed";

    public static void Seed(ModelBuilder model)
    {
        Add(model, 1, "C# fundamentals", "A quick check of C# types and control flow.",
            "Which C# keyword declares a Boolean value?", ["bool", "string", "decimal", "char"]);
        Add(model, 2, "HTTP basics", "Requests, responses, and useful status codes.",
            "Which status code means a resource was created?", ["201", "200", "404", "500"]);
        Add(model, 3, "REST principles", "Resources, HTTP methods, and idempotency.",
            "Which HTTP method replaces an existing resource?", ["PUT", "GET", "POST", "DELETE"]);
    }

    private static void Add(ModelBuilder model, int number, string title, string description,
        string questionText, string[] answers)
    {
        var quizId = Guid.Parse(new string((char)('0' + number), 32));
        var questionId = Guid.Parse($"10000000-0000-0000-0000-{number:000000000000}");
        model.Entity<Quiz>().HasData(new
        {
            Id = quizId, Title = title, Description = description, OwnerId = Owner,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        model.Entity<Question>().HasData(new
        {
            Id = questionId, QuizId = quizId, Text = questionText, Position = 0
        });
        for (var index = 0; index < answers.Length; index++)
        {
            model.Entity<AnswerOption>().HasData(new
            {
                Id = Guid.Parse($"20000000-0000-0000-0000-{number * 10 + index:000000000000}"),
                QuestionId = questionId, Text = answers[index], Position = index, IsCorrect = index == 0
            });
        }
    }
}
