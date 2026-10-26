using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuizMaster.Api.Auth;
using QuizMaster.Api.Contracts;
using QuizMaster.Api.Models;

namespace QuizMaster.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<MeResponse>> Register(RegisterRequest request)
    {
        if (ReservedDisplayNames.IsReserved(request.DisplayName))
        {
            ModelState.AddModelError("displayName", ReservedDisplayNames.Message);
            return ValidationProblem(ModelState);
        }

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim(),
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) return IdentityProblem(result);

        await userManager.AddToRoleAsync(user, Roles.User);

        await signInManager.SignInAsync(user, isPersistent: false);
        return Created("/api/auth/me", await ToMeResponse(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<MeResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null) return InvalidLogin();

        var result = await signInManager.PasswordSignInAsync(
            user, request.Password, isPersistent: request.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Account locked",
                detail: "Too many failed attempts. Try again in a few minutes.");
        if (!result.Succeeded) return InvalidLogin();

        return Ok(await ToMeResponse(user));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(await ToMeResponse(user));
    }

    private async Task<MeResponse> ToMeResponse(AppUser user) =>
        new(user.Id, user.Email!, user.DisplayName, [.. await userManager.GetRolesAsync(user)]);

    private ObjectResult InvalidLogin() =>
        Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password");

    private ActionResult IdentityProblem(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            // UserName is the email here, so this duplicates DuplicateEmail.
            if (error.Code == nameof(IdentityErrorDescriber.DuplicateUserName)) continue;

            var field = error.Code.StartsWith("Password") ? "password"
                : error.Code.Contains("Email") ? "email"
                : error.Code == UniqueDisplayNameValidator.ErrorCode ? "displayName"
                : string.Empty;
            ModelState.AddModelError(field, error.Description);
        }
        return ValidationProblem(ModelState);
    }
}
