using System.ComponentModel.DataAnnotations;

namespace QuizMaster.Api.Services.Documents;

public class DocumentOptions
{
    public const string SectionName = "Documents";

    // Relative paths are resolved against the API project folder.
    [Required]
    public string StoragePath { get; init; } = "App_Data/documents";

    [Range(200, 8000)]
    public int ChunkSize { get; init; } = 1000;

    [Range(0, 2000)]
    public int ChunkOverlap { get; init; } = 150;

    [Range(1, 10_000)]
    public int MaxChunks { get; init; } = 200;
}
