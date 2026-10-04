using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using NationalAirspace.Infrastructure.Services;

namespace NationalAirspace.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> to create SQL Server migrations without starting the web host.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=NationalAirspaceCms;Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
        return new ApplicationDbContext(options, new UtcClock(new ConfigurationBuilder().Build()));
    }
}
