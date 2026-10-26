using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.AI;

namespace QuizMaster.Api.Services.QuizGeneration;

public class QuizGenerationOptions
{
    public const string SectionName = "QuizGeneration";
    public const string RateLimitPolicy = "quiz-generation";

    // 0 means no limit.
    [Range(0, 3600)]
    public int TimeoutSeconds { get; init; } = 150;

    // How much the model thinks before each answer (Gemini calls it the thinking level).
    // Less is faster; leave it out to use the model's default.
    public ReasoningEffort? ReasoningEffort { get; init; }

    // Each generation makes several Gemini calls, so users get a few per window to protect the free tier.
    [Range(1, 1000)]
    public int PermitsPerUser { get; init; } = 5;

    [Range(1, 1440)]
    public int WindowMinutes { get; init; } = 10;
}
