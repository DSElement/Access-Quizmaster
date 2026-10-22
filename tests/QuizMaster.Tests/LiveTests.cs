using System.ClientModel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenAI;
using QuizMaster.Data;
using QuizMaster.Repositories;
using QuizMaster.Services;

namespace QuizMaster.Tests;

public sealed class MigrationTests
{
    [Fact]
    public void MigrationMatchesTheModelAndCreatesCascadingRelationships()
    {
        using var database = new QuizzesDbContext(new DbContextOptionsBuilder<QuizzesDbContext>()
            .UseSqlServer(LiveConfiguration.Read().GetConnectionString("QuizzesDb")).Options);
        Assert.False(database.Database.HasPendingModelChanges());
        var script = database.GetService<IMigrator>().GenerateScript();
        Assert.Contains("CREATE TABLE [Quizzes]", script);
        Assert.Contains("CREATE TABLE [Questions]", script);
        Assert.Contains("CREATE TABLE [AnswerOptions]", script);
        Assert.Equal(2, script.Split("ON DELETE CASCADE").Length - 1);
    }
}

public sealed class SqlServerTests
{
    [SqlServerFact]
    public async Task MigrationsPersistenceOrderingAndCascadeDeleteWorkOnSqlServer()
    {
        var options = OptionsForNewTestDatabase();
        await using var setup = new QuizzesDbContext(options);
        try
        {
            await setup.Database.MigrateAsync();
            var repository = new EfQuizRepository(setup);
            var quiz = await repository.CreateAsync("Persistence test", null, "alice",
                [TestData.Question("First question?"), TestData.Question("Second question?")], default);
            var questionIds = quiz.Questions.Select(q => q.Id).ToList();

            // A separate context simulates a new application process with no tracked objects.
            await using var restarted = new QuizzesDbContext(options);
            var freshRepository = new EfQuizRepository(restarted);
            var loaded = (await freshRepository.FindAsync(quiz.Id, default))!;
            Assert.Equal(new[] { "First question?", "Second question?" }, loaded.Questions.Select(q => q.Text));
            Assert.Equal(new[] { "Correct option", "Second option", "Third option", "Fourth option" }, loaded.Questions[0].Options.Select(o => o.Text));
            Assert.True((await freshRepository.ListAsync("alice", default)).Single(q => q.Id == quiz.Id).CanEdit);
            await freshRepository.DeleteAsync(loaded, default);
            Assert.False(await restarted.Questions.AnyAsync(q => q.QuizId == quiz.Id));
            Assert.False(await restarted.AnswerOptions.AnyAsync(o => questionIds.Contains(o.QuestionId)));
        }
        finally { await setup.Database.EnsureDeletedAsync(); }
    }

    [SqlServerFact]
    public async Task FailedGraphInsertRollsBackTheEntireQuiz()
    {
        var options = OptionsForNewTestDatabase();
        await using var database = new QuizzesDbContext(options);
        try
        {
            await database.Database.MigrateAsync();
            var invalid = TestData.Question("Invalid second question?");
            invalid.Options[0].Text = new string('x', 301);
            await Assert.ThrowsAsync<DbUpdateException>(() => new EfQuizRepository(database).CreateAsync(
                "Must not survive", null, "alice", [TestData.Question(), invalid], default));
            database.ChangeTracker.Clear();
            Assert.Equal(3, await database.Quizzes.CountAsync());
            Assert.Equal(3, await database.Questions.CountAsync());
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }

    private static DbContextOptions<QuizzesDbContext> OptionsForNewTestDatabase()
    {
        var connection = new SqlConnectionStringBuilder(LiveConfiguration.Read().GetConnectionString("QuizzesDb"))
        {
            InitialCatalog = $"AiLabsTests_{Guid.NewGuid():N}"
        };
        return new DbContextOptionsBuilder<QuizzesDbContext>().UseSqlServer(connection.ConnectionString).Options;
    }
}

public sealed class LocalModelTests
{
    [OllamaTheory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task LocalModelCompletesTheAgentWorkflow(int questionCount)
    {
        var settings = LiveConfiguration.Read().GetRequiredSection("Ollama").Get<OllamaOptions>()!;
        var provider = new OpenAIClient(new ApiKeyCredential("ollama"), new OpenAIClientOptions
        {
            Endpoint = new Uri(settings.Endpoint), NetworkTimeout = TimeSpan.FromSeconds(settings.TimeoutSeconds)
        });
        using var chat = provider.GetChatClient(settings.Model).AsIChatClient().AsBuilder().UseFunctionInvocation().Build();
        await using var database = TestData.Database();
        await database.Database.EnsureCreatedAsync();
        var generator = new OllamaQuizGenerator(chat, new EfQuizRepository(database), Options.Create(settings), NullLogger<OllamaQuizGenerator>.Instance);
        var source = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "library-policy.txt"));
        if (questionCount == 10) source = string.Concat(Enumerable.Repeat(source + "\n", 20))[..20000];
        var result = await generator.GenerateAsync(source, questionCount, default);
        Assert.Null(QuizValidation.Generated(result, questionCount));
        Assert.False(string.IsNullOrWhiteSpace(result.Title));
    }

}

internal static class LiveConfiguration
{
    public static IConfiguration Read() => new ConfigurationBuilder()
        .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "backend.appsettings.json"))
        .AddEnvironmentVariables().Build();
}

internal sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("AI_LABS_LIVE_SQL") != "1")
            Skip = "Set AI_LABS_LIVE_SQL=1 to test migrations and persistence against an isolated SQL Server database.";
    }
}

internal sealed class OllamaTheoryAttribute : TheoryAttribute
{
    public OllamaTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("AI_LABS_LIVE_AI") != "1")
            Skip = "Set AI_LABS_LIVE_AI=1 with Ollama running to test real local generation.";
    }
}
