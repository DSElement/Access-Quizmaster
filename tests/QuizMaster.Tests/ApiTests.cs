using System.Net;
using System.Net.Http.Json;
using QuizMaster.Contracts;

namespace QuizMaster.Tests;

public sealed class ApiTests
{
    [Fact]
    public async Task PublicReadsNeverExposeAnswerKeys()
    {
        using var app = new ApiFactory();
        using var client = app.Client();
        foreach (var path in new[] { "/quizzes", "/quizzes/11111111-1111-1111-1111-111111111111/play" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("isCorrect", await response.Content.ReadAsStringAsync());
            Assert.DoesNotContain("correctOptionId", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task WritesRequireIdentityAndApiScope()
    {
        using var app = new ApiFactory();
        using var anonymous = app.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/quizzes", Input())).StatusCode);
        using var withoutScope = app.Client("alice");
        withoutScope.DefaultRequestHeaders.Add("X-Test-No-Scope", "true");
        Assert.Equal(HttpStatusCode.Forbidden, (await withoutScope.PostAsJsonAsync("/quizzes", Input())).StatusCode);
    }

    [Fact]
    public async Task CrudPreservesQuestionOrderAndEnforcesOwnership()
    {
        using var app = new ApiFactory();
        using var alice = app.Client("alice");
        using var bob = app.Client("bob");
        var created = await alice.PostAsJsonAsync("/quizzes", Input());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var quiz = (await created.Content.ReadFromJsonAsync<QuizDetail>())!;
        Assert.EndsWith($"/quizzes/{quiz.Id}", created.Headers.Location!.ToString());
        Assert.Equal(new[] { "First question?", "Second question?" }, quiz.Questions.Select(q => q.Text));
        Assert.True(quiz.CanEdit);
        Assert.DoesNotContain("isCorrect", await created.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Forbidden, (await bob.PutAsJsonAsync($"/quizzes/{quiz.Id}", new { title = "Stolen title" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await bob.DeleteAsync($"/quizzes/{quiz.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await bob.PostAsJsonAsync($"/quizzes/{quiz.Id}/questions", TestData.Question())).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.PutAsJsonAsync($"/quizzes/{quiz.Id}", new { title = "Updated title", description = "Updated description" })).StatusCode);
        var updated = (await alice.GetFromJsonAsync<QuizDetail>($"/quizzes/{quiz.Id}"))!;
        Assert.Equal("Updated title", updated.Title);
        Assert.Equal(2, updated.Questions.Count);
        Assert.False((await bob.GetFromJsonAsync<QuizDetail>($"/quizzes/{quiz.Id}"))!.CanEdit);

        Assert.Equal(HttpStatusCode.NoContent, (await alice.DeleteAsync($"/quizzes/{quiz.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.GetAsync($"/quizzes/{quiz.Id}/play")).StatusCode);
    }

    [Fact]
    public async Task InvalidNestedQuestionDoesNotSavePartialQuiz()
    {
        using var app = new ApiFactory();
        using var client = app.Client("alice");
        var input = Input();
        input.Questions[1].Options[0].IsCorrect = false;
        var before = (await client.GetFromJsonAsync<List<QuizSummary>>("/quizzes"))!.Count;
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/quizzes", input)).StatusCode);
        Assert.Equal(before, (await client.GetFromJsonAsync<List<QuizSummary>>("/quizzes"))!.Count);
    }

    [Theory]
    [InlineData("{\"title\":\"Title\",\"questions\":null}")]
    [InlineData("{\"title\":\"Title\",\"questions\":[{\"text\":\"Question\",\"options\":null}]}")]
    [InlineData("{\"title\":\"Title\",\"questions\":[null]}")]
    [InlineData("{\"title\":\"Title\",\"questions\":[{\"text\":\"Question\",\"options\":[null,null]}]}")]
    [InlineData("{\"title\":\"  \",\"questions\":[]}")]
    public async Task MalformedInputIsRejected(string json)
    {
        using var app = new ApiFactory();
        using var client = app.Client("alice");
        var response = await client.PostAsync("/quizzes", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EvaluationRevealsAnswersOnlyAfterSubmission()
    {
        using var app = new ApiFactory();
        using var client = app.Client();
        const string quizId = "11111111-1111-1111-1111-111111111111";
        var play = (await client.GetFromJsonAsync<QuizDetail>($"/quizzes/{quizId}/play"))!;
        var question = play.Questions[0];
        var response = await client.PostAsJsonAsync($"/quizzes/{quizId}/evaluate", new SubmitQuizRequest
        {
            Answers = [new(question.Id, question.Options[0].Id)]
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<QuizResult>())!;
        Assert.Equal(100, result.ScorePercentage);
        Assert.Single(result.Results[0].Options, option => option.IsCorrect);
        var invalid = await client.PostAsJsonAsync($"/quizzes/{quizId}/evaluate", new SubmitQuizRequest
        {
            Answers = [new(question.Id, Guid.NewGuid())]
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task GenerationPersistsTheModelTitleAndAllowsAnExplicitTitle()
    {
        using var app = new ApiFactory();
        using var client = app.Client("alice");
        var request = new GenerateQuizRequest { SourceText = new string('a', 250), QuestionCount = 3 };
        var response = await client.PostAsJsonAsync("/quizzes/generate", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var quiz = (await response.Content.ReadFromJsonAsync<QuizDetail>())!;
        Assert.Equal("A generated title", quiz.Title);
        Assert.Equal(3, quiz.Questions.Count);
        request.Title = "My explicit title";
        var overridden = await client.PostAsJsonAsync("/quizzes/generate", request);
        Assert.Equal("My explicit title", (await overridden.Content.ReadFromJsonAsync<QuizDetail>())!.Title);
    }

    [Fact]
    public async Task GenerationFailureDoesNotPersistAnythingAndCrudStillWorks()
    {
        using var app = new ApiFactory { FailGeneration = true };
        using var client = app.Client("alice");
        var response = await client.PostAsJsonAsync("/quizzes/generate", new GenerateQuizRequest
        {
            SourceText = new string('a', 250), QuestionCount = 3
        });
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(3, (await client.GetFromJsonAsync<List<QuizSummary>>("/quizzes"))!.Count);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/quizzes", Input())).StatusCode);
    }

    [Fact]
    public async Task UnknownResourceReturnsNotFound()
    {
        using var app = new ApiFactory();
        using var client = app.Client("alice");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/quizzes/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/quizzes/{Guid.NewGuid()}/questions", TestData.Question())).StatusCode);
    }

    private static CreateQuizRequest Input() => new()
    {
        Title = "A complete quiz", Description = "A test quiz.",
        Questions = [TestData.Question("First question?"), TestData.Question("Second question?")]
    };
}
