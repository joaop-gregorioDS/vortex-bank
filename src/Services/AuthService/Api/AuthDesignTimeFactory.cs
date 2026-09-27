using Bankcore.Auth.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bankcore.Auth.Api;

public sealed class AuthDesignTimeFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=bankcore_auth;Username=bankcore;Password=bankcore-dev")
            .Options;
        return new AuthDbContext(options);
    }
}
