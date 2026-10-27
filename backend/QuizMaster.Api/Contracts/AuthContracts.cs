using System.ComponentModel.DataAnnotations;

namespace QuizMaster.Api.Contracts;

public record RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; init; } = "";

    [Required, StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; init; } = "";

    [Required]
    public string Password { get; init; } = "";
}

public record LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = "";

    [Required]
    public string Password { get; init; } = "";

    public bool RememberMe { get; init; }
}

public record UpdateProfileRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string DisplayName { get; init; } = "";
}

public record MeResponse(string Id, string Email, string DisplayName, IReadOnlyList<string> Roles);

public record AdminUserSummary(string Id, string Email, string DisplayName, IReadOnlyList<string> Roles);
