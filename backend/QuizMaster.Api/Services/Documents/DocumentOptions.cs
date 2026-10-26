using System.ComponentModel.DataAnnotations;

namespace QuizMaster.Api.Services.Documents;

public class DocumentOptions
{
    public const string SectionName = "Documents";

    // Relative paths are resolved against the API project folder.
    [Required]
    public string StoragePath { get; init; } = "App_Data/documents";

    [Range(200, 8000)]
    public int ChunkSize { get; init; } = 1500;

    [Range(0, 2000)]
    public int ChunkOverlap { get; init; } = 200;

    [Range(1, 10_000)]
    public int MaxChunks { get; init; } = 200;

    // Used instead of MaxChunks when the document's owner is an admin.
    [Range(1, 10_000)]
    public int AdminMaxChunks { get; init; } = 400;
}
