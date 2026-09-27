using Bankcore.Transactions.Domain;
using Bankcore.Transactions.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Bankcore.Transactions.Infrastructure.Tests;

public class SchemaTests
{
    [Fact]
    public void Production_compose_does_not_publish_databases()
    {
        var root = FindRoot();
        var prod = File.ReadAllText(Path.Combine(root, "docker", "docker-compose.prod.yml"));
        Assert.DoesNotContain("5432:5432", prod);
        Assert.DoesNotContain("6379:6379", prod);
        Assert.Contains("requirepass", prod);
        Assert.Contains("auth-net", prod);
        Assert.Contains("trans-net", prod);
        Assert.Contains("\"80:80\"", prod);
        Assert.Contains("\"443:443\"", prod);
    }

    [Fact]
    public async Task Ledger_trigger_rejects_update_and_delete()
    {
        await using var postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
        await postgres.StartAsync();
        var options = new DbContextOptionsBuilder<BankDbContext>().UseNpgsql(postgres.GetConnectionString()).Options;
        await using var db = new BankDbContext(options);
        await BankSchema.EnsureAsync(db, CancellationToken.None);

        var ledger = Guid.NewGuid();
        var journal = Guid.NewGuid();
        db.LedgerAccounts.Add(new LedgerAccount { Id = ledger, Kind = "deposit", Normal = NormalBalance.Credit, GuardNonNegative = true });
        db.JournalEntries.Add(new JournalEntry
        {
            Id = journal,
            Kind = JournalKind.Seed,
            BusinessDate = new DateOnly(2026, 9, 28),
            CreatedAtUtc = DateTime.UtcNow,
            ActorUserId = Guid.NewGuid(),
            Postings =
            [
                new Posting { Id = Guid.NewGuid(), JournalEntryId = journal, LedgerAccountId = ledger, Amount = 10m, Direction = EntryDirection.Credit },
                new Posting { Id = Guid.NewGuid(), JournalEntryId = journal, LedgerAccountId = ledger, Amount = 10m, Direction = EntryDirection.Debit }
            ]
        });
        await db.SaveChangesAsync();

        var update = async () => await db.Database.ExecuteSqlRawAsync("UPDATE postings SET \"Amount\" = 1");
        var exception = await Assert.ThrowsAsync<PostgresException>(update);
        Assert.Contains("imutáveis", exception.MessageText);
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Bankcore.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Raiz da solução não encontrada.");
    }
}
