using System.ComponentModel.DataAnnotations;

namespace QuizMaster.Api.Contracts;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, StringLength(100, MinimumLength = 2)] string DisplayName,
    [Required] string Password);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    bool RememberMe = false);

public record MeResponse(string Id, string Email, string DisplayName, IReadOnlyList<string> Roles);

public record AdminUserSummary(string Id, string Email, string DisplayName, IReadOnlyList<string> Roles);
