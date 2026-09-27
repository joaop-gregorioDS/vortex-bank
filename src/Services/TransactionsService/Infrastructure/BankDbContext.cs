using Bankcore.Transactions.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bankcore.Transactions.Infrastructure;

public enum DepositKind { Checking = 1, Savings = 2 }
public enum CardKind { Debit = 1, Credit = 2 }
public enum BoletoStatus { Open = 1, Paid = 2 }
public enum TedStatus { Scheduled = 1, Settled = 2, Rejected = 3 }
public enum PixKeyKind { Cpf = 1, Email = 2, Phone = 3, Random = 4 }

public sealed class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Cpf { get; set; } = "";
}

public sealed class LedgerAccount
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = "";
    public NormalBalance Normal { get; set; }
    public decimal ProjectedBalance { get; set; }
    public bool GuardNonNegative { get; set; }
}

public sealed class DepositAccount
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public DepositKind Kind { get; set; }
    public string Agency { get; set; } = "0001";
    public string Number { get; set; } = "";
    public Guid LedgerAccountId { get; set; }
}

public sealed class JournalEntry
{
    public Guid Id { get; set; }
    public JournalKind Kind { get; set; }
    public DateOnly BusinessDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid ActorUserId { get; set; }
    public string? CorrelationId { get; set; }
    public string? Reference { get; set; }
    public List<Posting> Postings { get; set; } = [];
}

public sealed class Posting
{
    public Guid Id { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid LedgerAccountId { get; set; }
    public decimal Amount { get; set; }
    public EntryDirection Direction { get; set; }
}

public sealed class IdempotencyRecord
{
    public Guid UserId { get; set; }
    public string Key { get; set; } = "";
    public string ResponseJson { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AuditEntry
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = "";
    public Guid UserId { get; set; }
    public string? Ip { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string Hash { get; set; } = "";
    public string Summary { get; set; } = "";
}

public sealed class PixKey
{
    public Guid Id { get; set; }
    public Guid DepositAccountId { get; set; }
    public PixKeyKind Kind { get; set; }
    public string Value { get; set; } = "";
    public bool Active { get; set; } = true;
}

public sealed class TedOrder
{
    public Guid Id { get; set; }
    public Guid FromDepositId { get; set; }
    public Guid ToDepositId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly ScheduledFor { get; set; }
    public TedStatus Status { get; set; }
    public Guid? FeeJournalId { get; set; }
    public Guid? SettlementJournalId { get; set; }
}

public sealed class Boleto
{
    public Guid Id { get; set; }
    public string Line { get; set; } = "";
    public string Beneficiary { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public BoletoStatus Status { get; set; }
    public bool External { get; set; }
    public Guid? PayeeCustomerId { get; set; }
}

public sealed class BankCard
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public CardKind Kind { get; set; }
    public string Pan { get; set; } = "";
    public string Holder { get; set; } = "";
    public string Expiry { get; set; } = "12/30";
    public string Cvv { get; set; } = "";
    public Guid? ReceivableLedgerId { get; set; }
}

public sealed class CardPurchaseRow
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public string Merchant { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly BusinessDate { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid? InvoiceId { get; set; }
}

public sealed class CardInvoice
{
    public Guid Id { get; set; }
    public Guid CardId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly ClosedOn { get; set; }
    public DateOnly DueOn { get; set; }
    public bool Paid { get; set; }
}

public sealed class SimulatorState
{
    public int Id { get; set; } = 1;
    public DateOnly BusinessDate { get; set; }
}

public sealed class ProcessedJob
{
    public string JobType { get; set; } = "";
    public DateOnly BusinessDate { get; set; }
    public Guid AccountId { get; set; }
}

public sealed class BankDbContext(DbContextOptions<BankDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LedgerAccount> LedgerAccounts => Set<LedgerAccount>();
    public DbSet<DepositAccount> DepositAccounts => Set<DepositAccount>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<Posting> Postings => Set<Posting>();
    public DbSet<IdempotencyRecord> Idempotency => Set<IdempotencyRecord>();
    public DbSet<AuditEntry> Audit => Set<AuditEntry>();
    public DbSet<PixKey> PixKeys => Set<PixKey>();
    public DbSet<TedOrder> Teds => Set<TedOrder>();
    public DbSet<Boleto> Boletos => Set<Boleto>();
    public DbSet<BankCard> Cards => Set<BankCard>();
    public DbSet<CardPurchaseRow> Purchases => Set<CardPurchaseRow>();
    public DbSet<CardInvoice> Invoices => Set<CardInvoice>();
    public DbSet<SimulatorState> Simulator => Set<SimulatorState>();
    public DbSet<ProcessedJob> Jobs => Set<ProcessedJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Cpf).IsUnique();
            entity.Property(item => item.Cpf).HasMaxLength(11);
            entity.Property(item => item.Name).HasMaxLength(160);
        });

        modelBuilder.Entity<LedgerAccount>(entity =>
        {
            entity.ToTable("ledger_accounts", table => table.HasCheckConstraint(
                "ck_ledger_nonnegative",
                "NOT \"GuardNonNegative\" OR \"ProjectedBalance\" >= 0"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProjectedBalance).HasColumnType("numeric(19,2)");
            entity.Property(item => item.Kind).HasMaxLength(40);
        });

        modelBuilder.Entity<DepositAccount>(entity =>
        {
            entity.ToTable("deposit_accounts");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Number).IsUnique();
            entity.Property(item => item.Agency).HasMaxLength(4);
            entity.Property(item => item.Number).HasMaxLength(8);
        });

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.ToTable("journal_entries");
            entity.HasKey(item => item.Id);
            entity.HasMany(item => item.Postings).WithOne().HasForeignKey(posting => posting.JournalEntryId);
            entity.Property(item => item.Reference).HasMaxLength(160);
            entity.Property(item => item.CorrelationId).HasMaxLength(80);
        });

        modelBuilder.Entity<Posting>(entity =>
        {
            entity.ToTable("postings");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Amount).HasColumnType("numeric(19,2)");
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("idempotency");
            entity.HasKey(item => new { item.UserId, item.Key });
            entity.Property(item => item.Key).HasMaxLength(80);
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("audit_entries");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Hash).HasMaxLength(64);
            entity.Property(item => item.CorrelationId).HasMaxLength(80);
            entity.Property(item => item.Summary).HasMaxLength(240);
            entity.Property(item => item.Ip).HasMaxLength(64);
        });

        modelBuilder.Entity<PixKey>(entity =>
        {
            entity.ToTable("pix_keys");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Value).IsUnique();
            entity.Property(item => item.Value).HasMaxLength(120);
        });

        modelBuilder.Entity<TedOrder>(entity =>
        {
            entity.ToTable("ted_orders");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Amount).HasColumnType("numeric(19,2)");
        });

        modelBuilder.Entity<Boleto>(entity =>
        {
            entity.ToTable("boletos");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Line).IsUnique();
            entity.Property(item => item.Line).HasMaxLength(48);
            entity.Property(item => item.Beneficiary).HasMaxLength(120);
            entity.Property(item => item.Amount).HasColumnType("numeric(19,2)");
        });

        modelBuilder.Entity<BankCard>(entity =>
        {
            entity.ToTable("cards");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Pan).HasMaxLength(16);
            entity.Property(item => item.Holder).HasMaxLength(80);
            entity.Property(item => item.Expiry).HasMaxLength(5);
            entity.Property(item => item.Cvv).HasMaxLength(3);
        });

        modelBuilder.Entity<CardPurchaseRow>(entity =>
        {
            entity.ToTable("card_purchases");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Amount).HasColumnType("numeric(19,2)");
            entity.Property(item => item.Merchant).HasMaxLength(80);
        });

        modelBuilder.Entity<CardInvoice>(entity =>
        {
            entity.ToTable("card_invoices");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Amount).HasColumnType("numeric(19,2)");
        });

        modelBuilder.Entity<SimulatorState>(entity =>
        {
            entity.ToTable("simulator_state");
            entity.HasKey(item => item.Id);
        });

        modelBuilder.Entity<ProcessedJob>(entity =>
        {
            entity.ToTable("processed_jobs");
            entity.HasKey(item => new { item.JobType, item.BusinessDate, item.AccountId });
            entity.Property(item => item.JobType).HasMaxLength(40);
        });
    }
}

public static class BankSchema
{
    public const string TriggerSql = """
        CREATE OR REPLACE FUNCTION bankcore_forbid_mutation() RETURNS trigger AS $$
        BEGIN
            RAISE EXCEPTION 'Lançamentos contábeis são imutáveis e auditáveis.';
        END;
        $$ LANGUAGE plpgsql;

        DROP TRIGGER IF EXISTS trg_postings_immutable ON postings;
        CREATE TRIGGER trg_postings_immutable
        BEFORE UPDATE OR DELETE ON postings
        FOR EACH ROW EXECUTE FUNCTION bankcore_forbid_mutation();

        DROP TRIGGER IF EXISTS trg_journal_immutable ON journal_entries;
        CREATE TRIGGER trg_journal_immutable
        BEFORE UPDATE OR DELETE ON journal_entries
        FOR EACH ROW EXECUTE FUNCTION bankcore_forbid_mutation();

        DROP TRIGGER IF EXISTS trg_audit_immutable ON audit_entries;
        CREATE TRIGGER trg_audit_immutable
        BEFORE UPDATE OR DELETE ON audit_entries
        FOR EACH ROW EXECUTE FUNCTION bankcore_forbid_mutation();
        """;

    public static async Task EnsureAsync(BankDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.MigrateAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync(TriggerSql, cancellationToken);
    }
}
