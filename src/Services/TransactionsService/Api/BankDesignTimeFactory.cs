using Bankcore.Transactions.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bankcore.Transactions.Api;

public sealed class BankDesignTimeFactory : IDesignTimeDbContextFactory<BankDbContext>
{
    public BankDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BankDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=bankcore_trans;Username=bankcore;Password=bankcore-dev")
            .Options;
        return new BankDbContext(options);
    }
}
