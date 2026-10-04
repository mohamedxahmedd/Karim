using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NationalAirspace.Application.Abstractions;
using NationalAirspace.Application.Features.Users;
using NationalAirspace.Infrastructure.Identity;
using NationalAirspace.Infrastructure.Persistence;
using NationalAirspace.Infrastructure.Persistence.Seeding;
using NationalAirspace.Infrastructure.Services;
using NationalAirspace.Infrastructure.Storage;

namespace NationalAirspace.Infrastructure;

public static class DependencyInjection
{
    public const string SqlServerProvider = "SqlServer";
    public const string SqliteProvider = "Sqlite";

    /// <param name="contentRootPath">Used to resolve relative SQLite database paths.</param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        var provider = configuration["Database:Provider"] ?? SqlServerProvider;
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured. Set ConnectionStrings__DefaultConnection.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (string.Equals(provider, SqliteProvider, StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(ResolveSqlitePath(connectionString, contentRootPath));
            else
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.EnableRetryOnFailure(maxRetryCount: 5);
                    sql.MigrationsHistoryTable("__EFMigrationsHistory");
                });
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredUniqueChars = 4;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        // Re-validate cookies frequently so deactivation / role changes take effect quickly.
        services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));

        services.AddSingleton<IClock, UtcClock>();
        services.AddSingleton<IHtmlSanitizer, HtmlSanitizerService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<DatabaseInitializer>();
        services.AddHostedService<AnnouncementExpiryJob>();

        services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database");
        return services;
    }

    private static string ResolveSqlitePath(string connectionString, string contentRootPath)
    {
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
        if (builder.DataSource is { Length: > 0 } source && source != ":memory:" && !source.StartsWith("file:", StringComparison.Ordinal)
            && builder.Mode != Microsoft.Data.Sqlite.SqliteOpenMode.Memory && !Path.IsPathRooted(source))
        {
            builder.DataSource = Path.Combine(contentRootPath, source);
            Directory.CreateDirectory(Path.GetDirectoryName(builder.DataSource)!);
        }
        return builder.ToString();
    }
}
