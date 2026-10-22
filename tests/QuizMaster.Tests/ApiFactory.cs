using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuizMaster.Data;
using QuizMaster.Services;

namespace QuizMaster.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = Guid.NewGuid().ToString();
    public bool FailGeneration { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<QuizzesDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<QuizzesDbContext>>();
            services.AddDbContext<QuizzesDbContext>(options => options.UseInMemoryDatabase(databaseName));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultForbidScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
            services.RemoveAll<IQuizGenerator>();
            services.AddSingleton<IQuizGenerator>(new TestGenerator(this));
        });
    }

    public HttpClient Client(string? user = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<QuizzesDbContext>().Database.EnsureCreated();
        if (user is not null) client.DefaultRequestHeaders.Add("X-Test-User", user);
        return client;
    }

    private sealed class TestGenerator(ApiFactory factory) : IQuizGenerator
    {
        public Task<GeneratedQuiz> GenerateAsync(string sourceText, int questionCount, CancellationToken cancellationToken)
        {
            if (factory.FailGeneration) throw new QuizGenerationException("The test model did not produce a quiz.");
            return Task.FromResult(new GeneratedQuiz("A generated title", Enumerable.Range(0, questionCount)
                .Select(index => TestData.Question($"Generated question {index + 1}?")).ToList()));
        }
    }
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var user))
            return Task.FromResult(AuthenticateResult.NoResult());
        List<Claim> claims = [new("oid", user.ToString())];
        if (!Request.Headers.ContainsKey("X-Test-No-Scope")) claims.Add(new("scp", "access_as_user"));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
