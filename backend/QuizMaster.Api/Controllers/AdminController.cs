using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizMaster.Api.Auth;
using QuizMaster.Api.Contracts;
using QuizMaster.Api.Data;

namespace QuizMaster.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminController(AppDbContext db) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserSummary>>> Users(CancellationToken cancellationToken)
    {
        // Users, UserRoles and Roles are DbSets that IdentityDbContext gives us.
        var users = await db.Users
            .OrderBy(u => u.Email)
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
}
