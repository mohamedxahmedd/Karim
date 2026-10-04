using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Common.Security;
using NationalAirspace.Infrastructure.Identity;

namespace NationalAirspace.Infrastructure.Persistence.Seeding;

/// <summary>Applies the schema and seeds roles, the initial administrator and baseline CMS content.</summary>
public sealed class DatabaseInitializer(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IConfiguration configuration,
    IFileStorage storage,
    IClock clock,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (db.Database.IsSqlServer())
        {
            if (configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
                await db.Database.MigrateAsync(ct);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(ct);
        }

        await SeedRolesAsync();
        await SeedAdministratorAsync();
        await new ContentSeeder(db).SeedAsync(ct);
        if (configuration.GetValue("Seed:SampleContent", false))
            await new SampleContentSeeder(db, storage, clock).SeedAsync(ct);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole(role));
        }
    }

    /// <summary>Creates the first SuperAdmin from configuration (never hard-coded) when no SuperAdmin exists.</summary>
    private async Task SeedAdministratorAsync()
    {
        if ((await userManager.GetUsersInRoleAsync(Roles.SuperAdmin)).Count > 0) return;

        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No SuperAdmin exists and Seed:AdminEmail / Seed:AdminPassword are not configured. " +
                              "Set them (user-secrets or environment variables) to create the initial administrator.");
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = configuration["Seed:AdminFullName"] ?? "System Administrator",
                IsActive = true,
                CreatedAtUtc = clock.UtcNow,
                LockoutEnabled = true,
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError("Initial administrator could not be created: {Errors}", string.Join("; ", result.Errors.Select(e => e.Code)));
                return;
            }
        }
        await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
        logger.LogInformation("Initial SuperAdmin account created.");
    }
}
