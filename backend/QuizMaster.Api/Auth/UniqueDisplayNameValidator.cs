using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Models;

namespace QuizMaster.Api.Auth;

public class UniqueDisplayNameValidator : IUserValidator<AppUser>
{
    public const string ErrorCode = "DuplicateDisplayName";

    public async Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user)
    {
        var normalized = AppUser.Normalize(user.DisplayName);
        var taken = await manager.Users.AnyAsync(u => u.NormalizedDisplayName == normalized && u.Id != user.Id);

        return taken
            ? IdentityResult.Failed(new IdentityError { Code = ErrorCode, Description = "That display name is already taken." })
            : IdentityResult.Success;
    }
}
