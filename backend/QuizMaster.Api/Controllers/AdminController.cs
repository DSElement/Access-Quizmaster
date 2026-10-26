using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Auth;
using QuizMaster.Api.Contracts;
using QuizMaster.Api.Data;
using QuizMaster.Api.Models;

namespace QuizMaster.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminController(AppDbContext db, UserManager<AppUser> userManager) : ControllerBase
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

        // Users, UserRoles and Roles are DbSets that IdentityDbContext gives us.
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
}
