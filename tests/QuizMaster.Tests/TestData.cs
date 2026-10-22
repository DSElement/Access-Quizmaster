using Microsoft.EntityFrameworkCore;
using QuizMaster.Contracts;
using QuizMaster.Data;
using QuizMaster.Models;

namespace QuizMaster.Tests;

internal static class TestData
{
    public static CreateQuestionRequest Question(string text = "What is the correct option?") => new()
    {
        Text = text,
        Options =
        [
            new() { Text = "Correct option", IsCorrect = true },
            new() { Text = "Second option" },
            new() { Text = "Third option" },
            new() { Text = "Fourth option" }
        ]
    };

    public static Quiz Quiz(int count = 3) => new()
    {
        Title = "Grading test", OwnerId = "alice",
        Questions = Enumerable.Range(0, count).Select(index => new Question
        {
            Text = $"Question number {index + 1}?", Position = index,
            Options = [new() { Text = "Yes", Position = 0, IsCorrect = true }, new() { Text = "No", Position = 1 }]
        }).ToList()
    };

    public static QuizzesDbContext Database() => new(new DbContextOptionsBuilder<QuizzesDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
