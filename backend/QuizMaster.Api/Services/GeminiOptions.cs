using System.ComponentModel.DataAnnotations;

namespace QuizMaster.Api.Services;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    [Required(ErrorMessage = "Gemini:ApiKey is missing. Set it with: dotnet user-secrets set \"Gemini:ApiKey\" \"<your key>\"")]
    public string ApiKey { get; init; } = "";

    [Required, Url]
    public string Endpoint { get; init; } = "";

    [Required]
    public string ChatModel { get; init; } = "";

    [Required]
    public string EmbeddingModel { get; init; } = "";
}
