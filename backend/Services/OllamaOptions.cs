using System.ComponentModel.DataAnnotations;

namespace QuizMaster.Services;

public sealed class OllamaOptions
{
    [Required, Url]
    public string Endpoint { get; set; } = "";

    [Required]
    public string Model { get; set; } = "";

    [Range(1, 3600)]
    public int TimeoutSeconds { get; set; }
}
