using System.ClientModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using OpenAI;
using QuizMaster.Data;
using QuizMaster.Repositories;
using QuizMaster.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.SingleLine = true);
var clientId = builder.Configuration["AzureAd:ClientId"]
    ?? throw new InvalidOperationException("AzureAd:ClientId is required.");
var scope = $"api://{clientId}/access_as_user";
var frontendOrigin = builder.Configuration["FrontendOrigin"]
    ?? throw new InvalidOperationException("FrontendOrigin is required.");

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "QuizMaster API", Version = "v1" });
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize"),
                TokenUrl = new Uri("https://login.microsoftonline.com/consumers/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string> { [scope] = "Access QuizMaster as you" }
            }
        }
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" } }] = [scope]
    });
});
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(frontendOrigin).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options => options.MapInboundClaims = false);
builder.Services.AddAuthorization(options => options.AddPolicy("QuizWriter", policy =>
    policy.RequireAuthenticatedUser().RequireClaim("oid").RequireAssertion(context =>
        context.User.FindFirst("scp")?.Value.Split(' ').Contains("access_as_user") == true)));

builder.Services.AddDbContext<QuizzesDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("QuizzesDb")
        ?? throw new InvalidOperationException("ConnectionStrings:QuizzesDb is required.")));
builder.Services.AddScoped<IQuizRepository, EfQuizRepository>();
builder.Services.AddOptions<OllamaOptions>().BindConfiguration("Ollama")
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddSingleton<IChatClient>(services =>
{
    var options = services.GetRequiredService<IOptions<OllamaOptions>>().Value;
    var client = new OpenAIClient(new ApiKeyCredential("ollama"), new OpenAIClientOptions
    {
        Endpoint = new Uri(options.Endpoint), NetworkTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
    });
    return client.GetChatClient(options.Model).AsIChatClient().AsBuilder().UseFunctionInvocation().Build();
});
builder.Services.AddScoped<IQuizGenerator, OllamaQuizGenerator>();

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId(clientId);
        options.OAuthUsePkce();
        options.OAuthScopeSeparator(" ");
    });
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.Run();

public partial class Program;
