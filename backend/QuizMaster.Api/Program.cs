using System.ClientModel;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using QuizMaster.Api.Auth;
using QuizMaster.Api.Data;
using QuizMaster.Api.Models;
using QuizMaster.Api.Services;
using QuizMaster.Api.Services.Documents;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
        // Validation errors use the JSON names ("questions[0].text"), not the C# ones ("Questions[0].Text").
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("QuizMaster")
    ?? throw new InvalidOperationException("Connection string 'QuizMaster' is missing.");

builder.Services.AddDbContext<AppDbContext>(options => options
    // Avoid warning in API log, (because of loading questions and options in a single query)
    .UseNpgsql(connectionString, npgsql => npgsql
        .UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
        // Maps Pgvector.Vector to the vector column type and enables CosineDistance() in LINQ
        .UseVector())
    // `CreatedAt` => `created_at`
    .UseSnakeCaseNamingConvention()
    .UseSeeding((context, _) => SeedData.Seed((AppDbContext)context))
    .UseAsyncSeeding((context, _, cancellationToken) => SeedData.SeedAsync((AppDbContext)context, cancellationToken)));

// Fails at startup (not on the first upload) when the API key or another setting is missing.
builder.Services.AddOptions<GeminiOptions>()
    .BindConfiguration(GeminiOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<DocumentOptions>()
    .BindConfiguration(DocumentOptions.SectionName)
    .ValidateDataAnnotations()
    .Validate(o => o.ChunkOverlap < o.ChunkSize / 2, "Documents:ChunkOverlap must be less than half of Documents:ChunkSize.")
    .ValidateOnStart();

// Gemini speaks the OpenAI API, so the OpenAI SDK only needs a different endpoint and key.
builder.Services.AddSingleton(services =>
{
    var gemini = services.GetRequiredService<IOptions<GeminiOptions>>().Value;
    return new OpenAIClient(
        new ApiKeyCredential(gemini.ApiKey),
        new OpenAIClientOptions { Endpoint = new Uri(gemini.Endpoint) });
});

builder.Services
    .AddEmbeddingGenerator(services => services.GetRequiredService<OpenAIClient>()
        .GetEmbeddingClient(services.GetRequiredService<IOptions<GeminiOptions>>().Value.EmbeddingModel)
        .AsIEmbeddingGenerator())
    // gemini-embedding-001 returns 3072 numbers by default; ask for the size the database column expects.
    .ConfigureOptions(options => options.Dimensions ??= DocumentChunk.EmbeddingDimensions);

builder.Services.AddSingleton<DocumentStorage>();
builder.Services.AddSingleton<DocumentQueue>();
builder.Services.AddScoped<DocumentIndexer>();
builder.Services.AddScoped<DocumentSearch>();
builder.Services.AddHostedService<DocumentIndexingWorker>();

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole>()
    .AddUserValidator<UniqueDisplayNameValidator>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "QuizMaster.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest            // local dev runs on plain http
        : CookieSecurePolicy.Always;                  // production: HTTPS only
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin))
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

var app = builder.Build();

await IdentitySeeder.SeedAsync(app.Services);

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
}

app.MapControllers();
app.MapHealthChecks("/api/health").AllowAnonymous();

app.Run();

public partial class Program;
