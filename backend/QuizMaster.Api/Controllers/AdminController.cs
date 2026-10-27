using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Auth;
using QuizMaster.Api.Contracts;
using QuizMaster.Api.Data;
using QuizMaster.Api.Models;
using QuizMaster.Api.Services.Documents;

namespace QuizMaster.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminController(
    AppDbContext db,
    UserManager<AppUser> userManager,
    DocumentStorage storage,
    ILogger<AdminController> logger) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserSummary>>> Users(
        [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = AppUser.Normalize(search);
            query = query.Where(u => u.NormalizedDisplayName.Contains(term) || u.NormalizedEmail!.Contains(term));
        }

        var users = await query
            .OrderBy(u => u.DisplayName)
            .Select(u => new AdminUserSummary(
                u.Id,
                u.Email!,
                u.DisplayName,
                db.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
                    .ToList()))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPost("users/{id}/promote")]
    public async Task<IActionResult> Promote(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (await userManager.IsInRoleAsync(user, Roles.Admin)) return NoContent();

        var result = await userManager.AddToRoleAsync(user, Roles.Admin);
        return result.Succeeded
            ? NoContent()
            : Problem(title: "Could not promote the user", detail: string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> Remove(string id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (await userManager.IsInRoleAsync(user, Roles.Admin)) return Forbid();

        var storedFiles = await db.Documents
            .Where(d => d.OwnerId == user.Id)
            .Select(d => d.StoredFileName)
            .ToListAsync(cancellationToken);

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return Problem(title: "Could not delete the user", detail: string.Join(" ", result.Errors.Select(e => e.Description)));

        foreach (var storedFile in storedFiles)
        {
            try
            {
                storage.Delete(storedFile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "Could not delete stored file {StoredFileName} of deleted user {UserId}", storedFile, user.Id);
            }
        }

        return NoContent();
    }
}
