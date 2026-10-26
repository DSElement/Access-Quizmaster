using Microsoft.AspNetCore.Identity;
using QuizMaster.Api.Auth;
using QuizMaster.Api.Models;

namespace QuizMaster.Api.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeeder));

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var email = config["Seed:AdminEmail"];
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No admin account seeded: set Seed:AdminEmail and Seed:AdminPassword (user-secrets).");
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null) return;

        var admin = new AppUser { UserName = email, Email = email, DisplayName = "Admin", EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Could not create the admin account: {errors}");
        }

        await userManager.AddToRolesAsync(admin, [Roles.Admin, Roles.User]);
        logger.LogInformation("Seeded admin account {Email}.", email);
    }
}
