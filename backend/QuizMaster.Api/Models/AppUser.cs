using Microsoft.AspNetCore.Identity;

namespace QuizMaster.Api.Models;

public class AppUser : IdentityUser
{
    public required string DisplayName { get; set; }
}
