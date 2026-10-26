using Microsoft.AspNetCore.Identity;

namespace QuizMaster.Api.Models;

public class AppUser : IdentityUser
{
    public required string DisplayName { get; set; }

    public string NormalizedDisplayName { get; set; } = "";

    public static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
