using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Errors are returned as RFC 9457 "Problem Details" JSON instead of HTML or empty bodies.
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("QuizMaster")
    ?? throw new InvalidOperationException("Connection string 'QuizMaster' is missing.");

builder.Services.AddDbContext<AppDbContext>(options => options
    // Avoid warning in API log, (because of loading questions and options in a single query)
    .UseNpgsql(connectionString, npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery))
    // `CreatedAt` => `created_at`
    .UseSnakeCaseNamingConvention()
    // Runs when migrations are applied
    .UseSeeding((context, _) => SeedData.Seed((AppDbContext)context))
    .UseAsyncSeeding((context, _, cancellationToken) => SeedData.SeedAsync((AppDbContext)context, cancellationToken)));

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // the machine-readable API description: /openapi/v1.json
    app.MapScalarApiReference();    // an interactive UI on top of it: /scalar
}

app.MapControllers();
app.MapHealthChecks("/api/health");

app.Run();

public partial class Program;
