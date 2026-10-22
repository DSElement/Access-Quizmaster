using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QuizMaster.Contracts;
using QuizMaster.Repositories;
using QuizMaster.Services;

namespace QuizMaster.Tests;

public sealed class AgentTests
{
    [Fact]
    public async Task AgentReadsTitlesAndSubmitsStructuredQuestionsThroughRealMiddleware()
    {
        await using var database = TestData.Database();
        await database.Database.EnsureCreatedAsync();
        var questions = Enumerable.Range(0, 3).Select(index => TestData.Question($"Source question {index + 1}?")).ToList();
        questions[0].Options[0].IsCorrect = false;
        questions[0].Options[2].IsCorrect = true;
        var raw = new ScriptedClient(
            messages =>
            {
                Assert.DoesNotContain(messages, message => message.Text.Contains(new string('a', 200)));
                return ToolCall("list", "get_quiz_context", []);
            },
            messages =>
            {
                Assert.Contains("C# fundamentals", JsonSerializer.Serialize(LastToolResult(messages).Result));
                Assert.Contains(new string('a', 200), JsonSerializer.Serialize(LastToolResult(messages).Result));
                return ToolCall("submit", "create_quiz", Arguments(questions));
            });
        using var client = raw.AsBuilder().UseFunctionInvocation().Build();
        var result = await Generator(client, new EfQuizRepository(database)).GenerateAsync(new string('a', 200), 3, default);
        Assert.Equal("Source-based title", result.Title);
        Assert.Equal(3, result.Questions.Count);
        Assert.True(result.Questions[0].Options[2].IsCorrect);
        Assert.Single(result.Questions[0].Options, option => option.IsCorrect);
        Assert.Equal(2, raw.CallCount);
        Assert.Equal(new[] { "get_quiz_context" }, raw.ToolNames[0]);
        Assert.Equal(new[] { "create_quiz" }, raw.ToolNames[1]);
        // Tools capture a result; only the controller is allowed to persist it.
        Assert.Equal(3, database.Quizzes.Count());
    }

    [Fact]
    public async Task InvalidToolArgumentsAreReportedToTheModelForCorrection()
    {
        await using var database = TestData.Database();
        await database.Database.EnsureCreatedAsync();
        var valid = Enumerable.Range(0, 3).Select(_ => TestData.Question()).ToList();
        var invalid = Enumerable.Range(0, 3).Select(_ => TestData.Question()).ToList();
        invalid[0].Text = "";
        var raw = new ScriptedClient(
            _ => ToolCall("list", "get_quiz_context", []),
            _ => ToolCall("bad", "create_quiz", Arguments(invalid)),
            messages =>
            {
                Assert.Contains("Question text", JsonSerializer.Serialize(LastToolResult(messages).Result));
                return ToolCall("fixed", "create_quiz", Arguments(valid));
            });
        using var client = raw.AsBuilder().UseFunctionInvocation().Build();
        var result = await Generator(client, new EfQuizRepository(database)).GenerateAsync(new string('a', 200), 3, default);
        Assert.All(result.Questions, question => Assert.Single(question.Options, option => option.IsCorrect));
    }

    [Fact]
    public async Task PlainTextIsNotAcceptedAsAReplacementForTheFinalToolCall()
    {
        await using var database = TestData.Database();
        var raw = new ScriptedClient(_ => Text("Here is a quiz in prose."));
        using var client = raw.AsBuilder().UseFunctionInvocation().Build();
        await Assert.ThrowsAsync<QuizGenerationException>(() =>
            Generator(client, new EfQuizRepository(database)).GenerateAsync(new string('a', 200), 3, default));
    }

    [Fact]
    public async Task ModelCannotSubmitBeforeReadingContext()
    {
        await using var database = TestData.Database();
        var raw = new ScriptedClient(
            _ => ToolCall("submit", "create_quiz", Arguments(Enumerable.Range(0, 3).Select(_ => TestData.Question()).ToList())),
            messages =>
            {
                Assert.Contains("not found", JsonSerializer.Serialize(LastToolResult(messages).Result), StringComparison.OrdinalIgnoreCase);
                return Text("Unable to finish.");
            });
        using var client = raw.AsBuilder().UseFunctionInvocation().Build();
        await Assert.ThrowsAsync<QuizGenerationException>(() =>
            Generator(client, new EfQuizRepository(database)).GenerateAsync(new string('a', 200), 3, default));
    }

    private static OllamaQuizGenerator Generator(IChatClient client, IQuizRepository repository) => new(
        client, repository, Options.Create(new OllamaOptions { TimeoutSeconds = 10 }), NullLogger<OllamaQuizGenerator>.Instance);

    private static Dictionary<string, object?> Arguments(List<CreateQuestionRequest> questions) => new()
    {
        ["title"] = "Source-based title", ["questions"] = JsonSerializer.SerializeToElement(questions.Select(question => new
        {
            text = question.Text,
            optionA = question.Options[0].Text,
            optionB = question.Options[1].Text,
            optionC = question.Options[2].Text,
            optionD = question.Options[3].Text,
            correctOption = "ABCD"[question.Options.FindIndex(option => option.IsCorrect)].ToString()
        }))
    };

    private static ChatResponse ToolCall(string id, string name, Dictionary<string, object?> arguments) =>
        new(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent(id, name, arguments)]))
        { FinishReason = ChatFinishReason.ToolCalls };

    private static ChatResponse Text(string text) => new(new ChatMessage(ChatRole.Assistant, text))
        { FinishReason = ChatFinishReason.Stop };

    private static FunctionResultContent LastToolResult(IReadOnlyList<ChatMessage> messages) =>
        messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().Last();

    private sealed class ScriptedClient(params Func<IReadOnlyList<ChatMessage>, ChatResponse>[] steps) : IChatClient
    {
        public int CallCount { get; private set; }
        public List<string[]> ToolNames { get; } = [];

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ToolNames.Add(options!.Tools!.Select(tool => tool.Name).ToArray());
            Assert.True(CallCount < steps.Length, "The agent made an unexpected additional model request.");
            return Task.FromResult(steps[CallCount++](messages.ToList()));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => serviceType.IsInstanceOfType(this) ? this : null;
        public void Dispose() { }
    }
}
