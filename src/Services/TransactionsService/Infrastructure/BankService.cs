using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bankcore.Transactions.Application;
using Bankcore.Transactions.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackExchange.Redis;

namespace Bankcore.Transactions.Infrastructure;

public sealed class BankService(BankDbContext db, IConnectionMultiplexer? redis) : IBank
{
    public static readonly Guid CashId = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    public static readonly Guid FeeId = Guid.Parse("00000000-0000-0000-0000-0000000000f1");
    public static readonly Guid InterestId = Guid.Parse("00000000-0000-0000-0000-0000000000e1");
    private static readonly string[] Merchants = ["Mercado", "Combustível", "Farmácia", "Streaming"];

    public async Task ProvisionAsync(Guid userId, string name, string cpf, CancellationToken cancellationToken)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            await EnsureHouseAsync(cancellationToken);
            await EnsureCustomerAsync(userId, name, cpf, null, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<HomeView> HomeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = await TodayAsync(cancellationToken);
        var accounts = await AccountsOf(userId, cancellationToken);
        if (accounts.Count == 0)
            return new HomeView(today, [], [], [], [], [], []);
        var cards = await CardsOf(userId, cancellationToken);
        var keys = await KeysOf(accounts, cancellationToken);
        var boletos = await BoletosOf(userId, cancellationToken);
        var teds = await TedsOf(userId, cancellationToken);
        var recent = await LinesAsync(userId, null, 8, cancellationToken);
        return new HomeView(today, accounts, cards, keys, boletos, teds, recent);
    }

    public Task<IReadOnlyList<StatementLine>> StatementAsync(Guid userId, Guid accountId, CancellationToken cancellationToken) =>
        LinesAsync(userId, accountId, 200, cancellationToken);

    public async Task<ReceiptView> ReceiptAsync(Guid userId, Guid journalId, CancellationToken cancellationToken)
    {
        var owned = await OwnedLedgers(userId, cancellationToken);
        var entry = await db.JournalEntries.Include(item => item.Postings).SingleOrDefaultAsync(item => item.Id == journalId, cancellationToken)
            ?? throw new BankingException("not_found", "Comprovante não encontrado.");
        if (!entry.Postings.Any(posting => owned.Contains(posting.LedgerAccountId)))
            throw new BankingException("not_found", "Comprovante não encontrado.");
        var amount = entry.Postings.Where(posting => posting.Direction == EntryDirection.Credit).Sum(posting => posting.Amount);
        return ToReceipt(entry, amount);
    }

    public async Task<PixKeyView> AddPixKeyAsync(Guid userId, string kind, string value, CancellationToken cancellationToken)
    {
        var checking = await CheckingAsync(userId, cancellationToken);
        var customer = await db.Customers.SingleAsync(item => item.Id == userId, cancellationToken);
        var parsed = ParseKey(kind, value, customer.Cpf);
        if (await db.PixKeys.AnyAsync(item => item.Value == parsed.Value, cancellationToken))
            throw new BankingException("pix_key_taken", "Esta chave PIX já está em uso.");
        var row = new PixKey { Id = Guid.NewGuid(), DepositAccountId = checking.Id, Kind = parsed.Kind, Value = parsed.Value };
        db.PixKeys.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        return new PixKeyView(row.Id, row.Kind.ToString().ToLowerInvariant(), row.Value);
    }

    public Task<ReceiptView> SendPixAsync(Guid userId, string key, decimal amount, string idempotencyKey, string correlationId, string? ip, CancellationToken cancellationToken) =>
        Once(userId, idempotencyKey, async () =>
        {
            var today = await TodayAsync(cancellationToken);
            var from = await CheckingAsync(userId, cancellationToken);
            var target = await db.PixKeys.SingleOrDefaultAsync(item => item.Active && item.Value == NormalizeKey(key), cancellationToken)
                ?? throw new BankingException("pix_key_missing", "Chave PIX não encontrada neste simulador.");
            var to = await db.DepositAccounts.SingleAsync(item => item.Id == target.DepositAccountId, cancellationToken);
            var sent = await SentTodayAsync(from.LedgerAccountId, today, cancellationToken);
            var fromLedger = await db.LedgerAccounts.SingleAsync(item => item.Id == from.LedgerAccountId, cancellationToken);
            var draft = Operations.Pix(from.LedgerAccountId, to.LedgerAccountId, amount, fromLedger.ProjectedBalance, sent, today);
            return await PostAsync(draft, userId, $"PIX {target.Value}", correlationId, ip, cancellationToken);
        }, cancellationToken);

    public Task<TedView> ScheduleTedAsync(Guid userId, string agency, string number, decimal amount, string idempotencyKey, string correlationId, string? ip, CancellationToken cancellationToken) =>
        Once(userId, idempotencyKey, async () =>
        {
            var today = await TodayAsync(cancellationToken);
            var from = await CheckingAsync(userId, cancellationToken);
            var destination = NormalizeAccount(number);
            if (agency != "0001")
                throw new BankingException("unknown_account", "Agência não encontrada. Neste simulador a agência é 0001.");
            var to = await db.DepositAccounts.SingleOrDefaultAsync(item => item.Agency == "0001" && item.Number == destination && item.Kind == DepositKind.Checking, cancellationToken)
                ?? throw new BankingException("unknown_account", "Conta de destino não encontrada.");
            if (to.Id == from.Id)
                throw new BankingException("self_transfer", "Não é possível enviar uma TED para a própria conta.");
            var ledger = await db.LedgerAccounts.SingleAsync(item => item.Id == from.LedgerAccountId, cancellationToken);
            var fee = Operations.TedFeeOnly(from.LedgerAccountId, FeeId, ledger.ProjectedBalance, amount, today);
            var receipt = await PostAsync(fee, userId, "Tarifa TED", correlationId, ip, cancellationToken);
            var order = new TedOrder
            {
                Id = Guid.NewGuid(),
                FromDepositId = from.Id,
                ToDepositId = to.Id,
                Amount = Money.Of(amount).Amount,
                ScheduledFor = Operations.NextBusinessDay(today),
                Status = TedStatus.Scheduled,
                FeeJournalId = receipt.JournalId
            };
            db.Teds.Add(order);
            await db.SaveChangesAsync(cancellationToken);
            return new TedView(order.Id, order.Amount, Operations.TedFee, "agendada", to.Agency, Display(to.Number), order.ScheduledFor);
        }, cancellationToken);

    public Task<ReceiptView> MoveSavingsAsync(Guid userId, string direction, decimal amount, string idempotencyKey, string correlationId, string? ip, CancellationToken cancellationToken) =>
        Once(userId, idempotencyKey, async () =>
        {
            var today = await TodayAsync(cancellationToken);
            var accounts = await db.DepositAccounts.Where(item => item.CustomerId == userId).ToListAsync(cancellationToken);
            var checking = accounts.Single(item => item.Kind == DepositKind.Checking);
            var savings = accounts.Single(item => item.Kind == DepositKind.Savings);
            var toSavings = direction == "para-poupanca";
            var from = toSavings ? checking : savings;
            var to = toSavings ? savings : checking;
            var ledger = await db.LedgerAccounts.SingleAsync(item => item.Id == from.LedgerAccountId, cancellationToken);
            var draft = Operations.SavingsMove(from.LedgerAccountId, to.LedgerAccountId, amount, ledger.ProjectedBalance, today);
            return await PostAsync(draft, userId, toSavings ? "Para poupança" : "Para conta corrente", correlationId, ip, cancellationToken);
        }, cancellationToken);

    public async Task<BoletoView> IssueBoletoAsync(Guid userId, decimal amount, DateOnly dueDate, string description, CancellationToken cancellationToken)
    {
        var money = Money.Of(amount);
        var today = await TodayAsync(cancellationToken);
        if (dueDate < today)
            throw new BankingException("boleto_expired", "O vencimento não pode ser anterior ao dia útil.");
        var customer = await db.Customers.SingleAsync(item => item.Id == userId, cancellationToken);
        var boleto = new Boleto
        {
            Id = Guid.NewGuid(),
            Line = "VB" + Guid.NewGuid().ToString("N")[..20],
            Beneficiary = string.IsNullOrWhiteSpace(description) ? customer.Name : description.Trim(),
            Amount = money.Amount,
            DueDate = dueDate,
            Status = BoletoStatus.Open,
            External = false,
            PayeeCustomerId = userId
        };
        db.Boletos.Add(boleto);
        await db.SaveChangesAsync(cancellationToken);
        return MapBoleto(boleto, userId);
    }

    public Task<ReceiptView> PayBoletoAsync(Guid userId, string line, string idempotencyKey, string correlationId, string? ip, CancellationToken cancellationToken) =>
        Once(userId, idempotencyKey, async () =>
        {
            var today = await TodayAsync(cancellationToken);
            var boleto = await db.Boletos.SingleOrDefaultAsync(item => item.Line == line.Trim(), cancellationToken)
                ?? throw new BankingException("boleto_unknown", "Este boleto não existe no Vortex Bank. Só quitamos cobranças emitidas aqui.");
            if (boleto.Status != BoletoStatus.Open)
                throw new BankingException("boleto_paid", "Este boleto já foi pago.");
            if (boleto.DueDate < today)
                throw new BankingException("boleto_expired", "Este boleto está vencido no dia útil corrente.");
            var checking = await CheckingAsync(userId, cancellationToken);
            var ledger = await db.LedgerAccounts.SingleAsync(item => item.Id == checking.LedgerAccountId, cancellationToken);
            JournalDraft draft;
            if (boleto.External || boleto.PayeeCustomerId is null)
                draft = Operations.ExternalBoleto(checking.LedgerAccountId, CashId, boleto.Amount, ledger.ProjectedBalance, today);
            else
            {
                var payee = await db.DepositAccounts.SingleAsync(item => item.CustomerId == boleto.PayeeCustomerId && item.Kind == DepositKind.Checking, cancellationToken);
                draft = Operations.CustomerBoleto(checking.LedgerAccountId, payee.LedgerAccountId, boleto.Amount, ledger.ProjectedBalance, today);
            }
            var receipt = await PostAsync(draft, userId, boleto.Beneficiary, correlationId, ip, cancellationToken);
            boleto.Status = BoletoStatus.Paid;
            await db.SaveChangesAsync(cancellationToken);
            return receipt;
        }, cancellationToken);

    public Task<ReceiptView> BuyAsync(Guid userId, Guid cardId, string merchant, decimal amount, string idempotencyKey, string correlationId, string? ip, CancellationToken cancellationToken) =>
        Once(userId, idempotencyKey, async () =>
        {
            if (!Merchants.Contains(merchant))
                throw new BankingException("merchant_unknown", "Escolha um comerciante da lista.");
            var today = await TodayAsync(cancellationToken);
            var card = await db.Cards.SingleOrDefaultAsync(item => item.Id == cardId && item.CustomerId == userId, cancellationToken)
                ?? throw new BankingException("not_found", "Cartão não encontrado.");
            var checking = await CheckingAsync(userId, cancellationToken);
            JournalDraft draft;
            if (card.Kind == CardKind.Debit)
            {
                var ledger = await db.LedgerAccounts.SingleAsync(item => item.Id == checking.LedgerAccountId, cancellationToken);
                draft = Operations.DebitPurchase(checking.LedgerAccountId, CashId, amount, ledger.ProjectedBalance, today);
            }
            else
            {
                var receivable = await db.LedgerAccounts.SingleAsync(item => item.Id == card.ReceivableLedgerId, cancellationToken);
                draft = Operations.CreditPurchase(receivable.Id, CashId, amount, receivable.ProjectedBalance, today);
            }
            var receipt = await PostAsync(draft, userId, merchant, correlationId, ip, cancellationToken);
            db.Purchases.Add(new CardPurchaseRow
            {
                Id = Guid.NewGuid(),
                CardId = card.Id,
                Merchant = merchant,
                Amount = Money.Of(amount).Amount,
                BusinessDate = today,
                JournalEntryId = receipt.JournalId
            });
            await db.SaveChangesAsync(cancellationToken);
            return receipt;
        }, cancellationToken);

    public Task<ReceiptView> PayInvoiceAsync(Guid userId, Guid invoiceId, string idempotencyKey, string correlationId, string? ip, CancellationToken cancellationToken) =>
        Once(userId, idempotencyKey, async () =>
        {
            var today = await TodayAsync(cancellationToken);
            var invoice = await db.Invoices.SingleOrDefaultAsync(item => item.Id == invoiceId, cancellationToken)
                ?? throw new BankingException("not_found", "Fatura não encontrada.");
            var card = await db.Cards.SingleAsync(item => item.Id == invoice.CardId, cancellationToken);
            if (card.CustomerId != userId)
                throw new BankingException("not_found", "Fatura não encontrada.");
            if (invoice.Paid)
                throw new BankingException("invoice_paid", "Esta fatura já foi paga.");
            var checking = await CheckingAsync(userId, cancellationToken);
            var checkingLedger = await db.LedgerAccounts.SingleAsync(item => item.Id == checking.LedgerAccountId, cancellationToken);
            var receivable = await db.LedgerAccounts.SingleAsync(item => item.Id == card.ReceivableLedgerId, cancellationToken);
            var draft = Operations.PayInvoice(checking.LedgerAccountId, receivable.Id, invoice.Amount, checkingLedger.ProjectedBalance, receivable.ProjectedBalance, today);
            var receipt = await PostAsync(draft, userId, "Pagamento de fatura", correlationId, ip, cancellationToken);
            invoice.Paid = true;
            await db.SaveChangesAsync(cancellationToken);
            return receipt;
        }, cancellationToken);

    public async Task<ClockView> AdvanceAsync(Guid userId, string correlationId, string? ip, CancellationToken cancellationToken)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
        var state = (await db.Simulator.FromSqlInterpolated($"SELECT * FROM simulator_state WHERE \"Id\" = 1 FOR UPDATE").ToListAsync(cancellationToken)).Single();
        var next = Operations.NextBusinessDay(state.BusinessDate);
        var due = await db.Teds.Where(item => item.Status == TedStatus.Scheduled && item.ScheduledFor <= next).ToListAsync(cancellationToken);
        foreach (var order in due)
        {
            var from = await db.DepositAccounts.SingleAsync(item => item.Id == order.FromDepositId, cancellationToken);
            var to = await db.DepositAccounts.SingleAsync(item => item.Id == order.ToDepositId, cancellationToken);
            var balance = (await db.LedgerAccounts.SingleAsync(item => item.Id == from.LedgerAccountId, cancellationToken)).ProjectedBalance;
            try
            {
                var draft = Operations.TedSettlement(from.LedgerAccountId, to.LedgerAccountId, order.Amount, balance, next);
                var receipt = await PostAsync(draft, userId, "TED liquidada", correlationId, ip, cancellationToken);
                order.Status = TedStatus.Settled;
                order.SettlementJournalId = receipt.JournalId;
            }
            catch (BankingException exception) when (exception.Code == "insufficient_funds")
            {
                var reversal = Operations.ReverseTedFee(from.LedgerAccountId, FeeId, next);
                await PostAsync(reversal, userId, "Estorno de tarifa TED", correlationId, ip, cancellationToken);
                order.Status = TedStatus.Rejected;
            }
        }

        var savings = await db.DepositAccounts.Where(item => item.Kind == DepositKind.Savings).ToListAsync(cancellationToken);
        foreach (var account in savings)
        {
            if (await db.Jobs.AnyAsync(job => job.JobType == "yield" && job.BusinessDate == next && job.AccountId == account.Id, cancellationToken))
                continue;
            var balance = (await db.LedgerAccounts.SingleAsync(item => item.Id == account.LedgerAccountId, cancellationToken)).ProjectedBalance;
            var yield = Operations.SavingsYield(InterestId, account.LedgerAccountId, balance, next);
            if (yield is not null)
                await PostAsync(yield, userId, "Rendimento da poupança", correlationId, ip, cancellationToken);
            db.Jobs.Add(new ProcessedJob { JobType = "yield", BusinessDate = next, AccountId = account.Id });
        }

        if (state.BusinessDate.Month != next.Month)
        {
            var cards = await db.Cards.Where(item => item.Kind == CardKind.Credit).ToListAsync(cancellationToken);
            foreach (var card in cards)
            {
                if (await db.Jobs.AnyAsync(job => job.JobType == "invoice" && job.BusinessDate == next && job.AccountId == card.Id, cancellationToken))
                    continue;
                var open = await db.Purchases.Where(item => item.CardId == card.Id && item.InvoiceId == null).ToListAsync(cancellationToken);
                if (open.Count > 0)
                {
                    var invoice = new CardInvoice
                    {
                        Id = Guid.NewGuid(),
                        CardId = card.Id,
                        Amount = open.Sum(item => item.Amount),
                        ClosedOn = next,
                        DueOn = new DateOnly(next.Year, next.Month, 10)
                    };
                    db.Invoices.Add(invoice);
                    foreach (var purchase in open)
                        purchase.InvoiceId = invoice.Id;
                }
                db.Jobs.Add(new ProcessedJob { JobType = "invoice", BusinessDate = next, AccountId = card.Id });
            }
        }

        state.BusinessDate = next;
        db.Audit.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            UserId = userId,
            Ip = ip,
            CreatedAtUtc = DateTime.UtcNow,
            Summary = $"Avanço do dia útil para {next:yyyy-MM-dd}",
            Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}|{next:yyyy-MM-dd}|{correlationId}")))
        });
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return new ClockView(next);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public static async Task SeedAsync(BankDbContext database, CancellationToken cancellationToken)
    {
        var bank = new BankService(database, null);
        await bank.EnsureHouseAsync(cancellationToken);
        var opened = new DateOnly(2026, 9, 1);
        await bank.EnsureCustomerAsync(DemoBook.Ana.Id, DemoBook.Ana.Name, DemoBook.Ana.Cpf, opened, cancellationToken);
        await bank.EnsureCustomerAsync(DemoBook.Bruno.Id, DemoBook.Bruno.Name, DemoBook.Bruno.Cpf, opened, cancellationToken);
        await bank.EnsureCustomerAsync(DemoBook.Carla.Id, DemoBook.Carla.Name, DemoBook.Carla.Cpf, opened, cancellationToken);
        if (await database.JournalEntries.AnyAsync(item => item.Kind == JournalKind.Pix, cancellationToken))
            return;

        var ana = await database.DepositAccounts.SingleAsync(item => item.CustomerId == DemoBook.Ana.Id && item.Kind == DepositKind.Checking, cancellationToken);
        var anaSavings = await database.DepositAccounts.SingleAsync(item => item.CustomerId == DemoBook.Ana.Id && item.Kind == DepositKind.Savings, cancellationToken);
        var bruno = await database.DepositAccounts.SingleAsync(item => item.CustomerId == DemoBook.Bruno.Id && item.Kind == DepositKind.Checking, cancellationToken);
        var carlaSavings = await database.DepositAccounts.SingleAsync(item => item.CustomerId == DemoBook.Carla.Id && item.Kind == DepositKind.Savings, cancellationToken);
        var carlaChecking = await database.DepositAccounts.SingleAsync(item => item.CustomerId == DemoBook.Carla.Id && item.Kind == DepositKind.Checking, cancellationToken);
        await bank.PostAsync(Operations.Pix(ana.LedgerAccountId, bruno.LedgerAccountId, 150m, 2500m, 0m, new DateOnly(2026, 9, 10)), DemoBook.Ana.Id, "PIX para Bruno Lima", "seed", null, cancellationToken);
        await bank.PostAsync(Operations.SavingsMove(ana.LedgerAccountId, anaSavings.LedgerAccountId, 400m, 2350m, new DateOnly(2026, 9, 12)), DemoBook.Ana.Id, "Para poupança", "seed", null, cancellationToken);
        var anaCard = await database.Cards.SingleAsync(item => item.CustomerId == DemoBook.Ana.Id && item.Kind == CardKind.Credit, cancellationToken);
        var purchase = await bank.PostAsync(Operations.CreditPurchase(anaCard.ReceivableLedgerId!.Value, CashId, 320m, 0m, new DateOnly(2026, 9, 18)), DemoBook.Ana.Id, "Farmácia", "seed", null, cancellationToken);
        var invoice = new CardInvoice { Id = Guid.NewGuid(), CardId = anaCard.Id, Amount = 320m, ClosedOn = new DateOnly(2026, 9, 20), DueOn = new DateOnly(2026, 10, 10) };
        database.Invoices.Add(invoice);
        database.Purchases.Add(new CardPurchaseRow { Id = Guid.NewGuid(), CardId = anaCard.Id, Merchant = "Farmácia", Amount = 320m, BusinessDate = new DateOnly(2026, 9, 18), JournalEntryId = purchase.JournalId, InvoiceId = invoice.Id });
        await bank.PostAsync(Operations.SavingsMove(carlaChecking.LedgerAccountId, carlaSavings.LedgerAccountId, 800m, 2500m, new DateOnly(2026, 9, 14)), DemoBook.Carla.Id, "Para poupança", "seed", null, cancellationToken);
        var yield = Operations.SavingsYield(InterestId, carlaSavings.LedgerAccountId, 800m, new DateOnly(2026, 9, 15));
        if (yield is not null)
            await bank.PostAsync(yield, DemoBook.Carla.Id, "Rendimento da poupança", "seed", null, cancellationToken);
        database.Boletos.AddRange(
            Catalog("Enel Energia", 186.40m, new DateOnly(2026, 10, 15)),
            Catalog("Sabesp Água", 94.20m, new DateOnly(2026, 10, 18)),
            Catalog("Fibra Internet", 129.90m, new DateOnly(2026, 10, 22)));
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureHouseAsync(CancellationToken cancellationToken)
    {
        await AddLedgerIfMissing(CashId, "cash", NormalBalance.Debit, false, cancellationToken);
        await AddLedgerIfMissing(FeeId, "fee", NormalBalance.Credit, false, cancellationToken);
        await AddLedgerIfMissing(InterestId, "interest", NormalBalance.Debit, false, cancellationToken);
        if (!await db.Simulator.AnyAsync(item => item.Id == 1, cancellationToken))
            db.Simulator.Add(new SimulatorState { Id = 1, BusinessDate = new DateOnly(2026, 9, 28) });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCustomerAsync(Guid userId, string name, string cpf, DateOnly? openedOn, CancellationToken cancellationToken)
    {
        var digits = Cpf.Digits(cpf);
        if (await db.Customers.AnyAsync(item => item.Id == userId, cancellationToken))
            return;
        await EnsureHouseAsync(cancellationToken);
        var today = openedOn ?? await TodayAsync(cancellationToken);
        db.Customers.Add(new Customer { Id = userId, Name = name.Trim(), Cpf = digits });
        var checkingLedger = Guid.NewGuid();
        var savingsLedger = Guid.NewGuid();
        var receivable = Guid.NewGuid();
        db.LedgerAccounts.AddRange(
            NewLedger(checkingLedger, "deposit", NormalBalance.Credit, true),
            NewLedger(savingsLedger, "deposit", NormalBalance.Credit, true),
            NewLedger(receivable, "receivable", NormalBalance.Debit, true));
        var existing = await db.DepositAccounts.CountAsync(cancellationToken);
        var checking = new DepositAccount { Id = Guid.NewGuid(), CustomerId = userId, Kind = DepositKind.Checking, Number = AccountNumber(existing), LedgerAccountId = checkingLedger };
        var savings = new DepositAccount { Id = Guid.NewGuid(), CustomerId = userId, Kind = DepositKind.Savings, Number = AccountNumber(existing + 1), LedgerAccountId = savingsLedger };
        db.DepositAccounts.AddRange(checking, savings);
        db.PixKeys.Add(new PixKey { Id = Guid.NewGuid(), DepositAccountId = checking.Id, Kind = PixKeyKind.Cpf, Value = digits });
        db.Cards.AddRange(
            new BankCard { Id = Guid.NewGuid(), CustomerId = userId, Kind = CardKind.Debit, Pan = Pan(checking.Number), Holder = name.Trim().ToUpperInvariant(), Cvv = "123" },
            new BankCard { Id = Guid.NewGuid(), CustomerId = userId, Kind = CardKind.Credit, Pan = Pan(savings.Number), Holder = name.Trim().ToUpperInvariant(), Cvv = "456", ReceivableLedgerId = receivable });
        await db.SaveChangesAsync(cancellationToken);
        var cash = await db.LedgerAccounts.SingleAsync(item => item.Id == CashId, cancellationToken);
        var draft = Operations.OpeningDeposit(CashId, checkingLedger, today, Operations.OpeningBalance);
        await PostAsync(draft, userId, "Saldo inicial do simulador", "seed", null, cancellationToken);
        _ = cash;
    }

    private async Task<ReceiptView> PostAsync(JournalDraft draft, Guid actor, string reference, string correlationId, string? ip, CancellationToken cancellationToken)
    {
        var locked = new Dictionary<Guid, LedgerAccount>();
        foreach (var id in draft.Postings.Select(posting => posting.LedgerAccountId).Distinct().OrderBy(id => id))
        {
            var row = (await db.LedgerAccounts.FromSqlInterpolated($"SELECT * FROM ledger_accounts WHERE \"Id\" = {id} FOR UPDATE").ToListAsync(cancellationToken)).Single();
            locked[row.Id] = row;
        }

        foreach (var account in locked.Values)
        {
            var next = decimal.Round(account.ProjectedBalance + draft.Delta(account.Id, account.Normal), 2);
            if (account.GuardNonNegative && next < 0)
                throw new BankingException("insufficient_funds", "Saldo insuficiente.");
            account.ProjectedBalance = next;
        }

        var created = DateTime.UtcNow;
        var entry = new JournalEntry
        {
            Id = Guid.NewGuid(),
            Kind = draft.Kind,
            BusinessDate = draft.BusinessDate,
            CreatedAtUtc = created,
            ActorUserId = actor,
            CorrelationId = correlationId,
            Reference = reference
        };
        foreach (var posting in draft.Postings)
        {
            entry.Postings.Add(new Posting
            {
                Id = Guid.NewGuid(),
                JournalEntryId = entry.Id,
                LedgerAccountId = posting.LedgerAccountId,
                Amount = posting.Amount,
                Direction = posting.Direction
            });
        }

        db.JournalEntries.Add(entry);
        var summary = $"{draft.Kind}:{reference}:{entry.Id:N}";
        db.Audit.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            UserId = actor,
            Ip = ip,
            CreatedAtUtc = created,
            Summary = summary,
            Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{entry.Id:N}|{actor:N}|{summary}|{created:O}|{ip}")))
        });
        await db.SaveChangesAsync(cancellationToken);
        var amount = draft.Postings.Where(posting => posting.Direction == EntryDirection.Credit).Sum(posting => posting.Amount);
        return ToReceipt(entry, amount);
    }

    private async Task<T> Once<T>(Guid userId, string key, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 80)
            throw new BankingException("idempotency_required", "Informe o cabeçalho Idempotency-Key.");
        var cached = await ReadCacheAsync(userId, key);
        if (cached is not null)
            return JsonSerializer.Deserialize<T>(cached)!;

        var existing = await db.Idempotency.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == userId && item.Key == key, cancellationToken);
        if (existing is not null && existing.ResponseJson.Length > 0)
            return JsonSerializer.Deserialize<T>(existing.ResponseJson)!;

        await using var locks = await HoldAsync(userId, cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var pending = true;
        try
        {
            db.Idempotency.Add(new IdempotencyRecord { UserId = userId, Key = key, ResponseJson = "", CreatedAtUtc = DateTime.UtcNow });
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUnique(exception))
            {
                await tx.RollbackAsync(cancellationToken);
                pending = false;
                db.ChangeTracker.Clear();
                var row = await db.Idempotency.AsNoTracking().SingleAsync(item => item.UserId == userId && item.Key == key, cancellationToken);
                if (row.ResponseJson.Length == 0)
                    throw new BankingException("idempotency_in_progress", "Esta operação já está em andamento.");
                return JsonSerializer.Deserialize<T>(row.ResponseJson)!;
            }

            var result = await work();
            var stored = await db.Idempotency.SingleAsync(item => item.UserId == userId && item.Key == key, cancellationToken);
            stored.ResponseJson = JsonSerializer.Serialize(result);
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            pending = false;
            await WriteCacheAsync(userId, key, stored.ResponseJson);
            return result;
        }
        catch
        {
            if (pending)
                await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<IAsyncDisposable> HoldAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (redis is not { IsConnected: true })
            return new Noop();
        var cache = redis.GetDatabase();
        var name = $"lock:customer:{userId}";
        if (!await cache.StringSetAsync(name, "1", TimeSpan.FromSeconds(8), When.NotExists))
            throw new BankingException("account_busy", "Há outra operação nesta conta. Tente de novo.");
        return new RedisRelease(cache, name);
    }

    private async Task<string?> ReadCacheAsync(Guid userId, string key)
    {
        if (redis is not { IsConnected: true })
            return null;
        var value = await redis.GetDatabase().StringGetAsync($"idem:{userId:N}:{key}");
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    private async Task WriteCacheAsync(Guid userId, string key, string json)
    {
        if (redis is not { IsConnected: true })
            return;
        await redis.GetDatabase().StringSetAsync($"idem:{userId:N}:{key}", json, TimeSpan.FromHours(24));
    }

    private async Task<DateOnly> TodayAsync(CancellationToken cancellationToken) =>
        (await db.Simulator.AsNoTracking().SingleAsync(item => item.Id == 1, cancellationToken)).BusinessDate;

    private async Task<DepositAccount> CheckingAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.DepositAccounts.SingleAsync(item => item.CustomerId == userId && item.Kind == DepositKind.Checking, cancellationToken);

    private async Task<decimal> SentTodayAsync(Guid ledgerId, DateOnly today, CancellationToken cancellationToken) =>
        await (from posting in db.Postings
               join journal in db.JournalEntries on posting.JournalEntryId equals journal.Id
               where journal.Kind == JournalKind.Pix && journal.BusinessDate == today
                   && posting.LedgerAccountId == ledgerId && posting.Direction == EntryDirection.Debit
               select posting.Amount).SumAsync(cancellationToken);

    private async Task<HashSet<Guid>> OwnedLedgers(Guid userId, CancellationToken cancellationToken)
    {
        var deposits = await db.DepositAccounts.Where(item => item.CustomerId == userId).Select(item => item.LedgerAccountId).ToListAsync(cancellationToken);
        var receivable = await db.Cards.Where(item => item.CustomerId == userId && item.ReceivableLedgerId != null).Select(item => item.ReceivableLedgerId!.Value).ToListAsync(cancellationToken);
        return deposits.Concat(receivable).ToHashSet();
    }

    private async Task<IReadOnlyList<AccountView>> AccountsOf(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await db.DepositAccounts.Where(item => item.CustomerId == userId).ToListAsync(cancellationToken);
        var ledgers = await db.LedgerAccounts.Where(item => rows.Select(row => row.LedgerAccountId).Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
        return rows.Select(row => new AccountView(row.Id, row.Kind == DepositKind.Checking ? "corrente" : "poupanca", row.Agency, Display(row.Number), ledgers[row.LedgerAccountId].ProjectedBalance)).ToList();
    }

    private async Task<IReadOnlyList<CardView>> CardsOf(Guid userId, CancellationToken cancellationToken)
    {
        var cards = await db.Cards.Where(item => item.CustomerId == userId).ToListAsync(cancellationToken);
        var views = new List<CardView>();
        foreach (var card in cards)
        {
            decimal? used = null;
            Guid? invoiceId = null;
            decimal? invoiceAmount = null;
            DateOnly? due = null;
            if (card.ReceivableLedgerId is Guid receivableId)
            {
                used = (await db.LedgerAccounts.SingleAsync(item => item.Id == receivableId, cancellationToken)).ProjectedBalance;
                var invoice = await db.Invoices.Where(item => item.CardId == card.Id && !item.Paid).OrderByDescending(item => item.DueOn).FirstOrDefaultAsync(cancellationToken);
                if (invoice is not null)
                {
                    invoiceId = invoice.Id;
                    invoiceAmount = invoice.Amount;
                    due = invoice.DueOn;
                }
            }
            views.Add(new CardView(card.Id, card.Kind == CardKind.Debit ? "debito" : "credito", card.Pan, card.Holder, card.Expiry, card.Cvv, card.Kind == CardKind.Credit ? Operations.CreditLimit : null, used, invoiceId, invoiceAmount, due));
        }
        return views;
    }

    private async Task<IReadOnlyList<PixKeyView>> KeysOf(IReadOnlyList<AccountView> accounts, CancellationToken cancellationToken)
    {
        var ids = accounts.Select(account => account.Id).ToArray();
        var keys = await db.PixKeys.Where(item => ids.Contains(item.DepositAccountId) && item.Active).ToListAsync(cancellationToken);
        return keys.Select(item => new PixKeyView(item.Id, item.Kind.ToString().ToLowerInvariant(), item.Value)).ToList();
    }

    private async Task<IReadOnlyList<BoletoView>> BoletosOf(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await db.Boletos.Where(item => item.Status == BoletoStatus.Open || item.PayeeCustomerId == userId).OrderBy(item => item.DueDate).ToListAsync(cancellationToken);
        return rows.Select(item => MapBoleto(item, userId)).ToList();
    }

    private async Task<IReadOnlyList<TedView>> TedsOf(Guid userId, CancellationToken cancellationToken)
    {
        var deposits = await db.DepositAccounts.Where(item => item.CustomerId == userId).Select(item => item.Id).ToListAsync(cancellationToken);
        var rows = await db.Teds.Where(item => deposits.Contains(item.FromDepositId)).OrderByDescending(item => item.ScheduledFor).ToListAsync(cancellationToken);
        var views = new List<TedView>();
        foreach (var row in rows)
        {
            var to = await db.DepositAccounts.SingleAsync(item => item.Id == row.ToDepositId, cancellationToken);
            views.Add(new TedView(row.Id, row.Amount, Operations.TedFee, row.Status switch
            {
                TedStatus.Settled => "liquidada",
                TedStatus.Rejected => "recusada",
                _ => "agendada"
            }, to.Agency, Display(to.Number), row.ScheduledFor));
        }
        return views;
    }

    private async Task<IReadOnlyList<StatementLine>> LinesAsync(Guid userId, Guid? accountId, int take, CancellationToken cancellationToken)
    {
        var accounts = await db.DepositAccounts.Where(item => item.CustomerId == userId).ToListAsync(cancellationToken);
        if (accountId is Guid chosen && accounts.All(item => item.Id != chosen))
            throw new BankingException("not_found", "Conta não encontrada.");
        var ledgerIds = accounts.Where(item => accountId is null || item.Id == accountId).Select(item => item.LedgerAccountId).ToArray();
        var rows = await (from posting in db.Postings
                          join journal in db.JournalEntries on posting.JournalEntryId equals journal.Id
                          where ledgerIds.Contains(posting.LedgerAccountId)
                          orderby journal.CreatedAtUtc descending
                          select new { journal, posting }).Take(take).ToListAsync(cancellationToken);
        return rows.Select(row => new StatementLine(
            row.journal.Id,
            row.journal.BusinessDate,
            row.journal.CreatedAtUtc,
            row.journal.Kind.ToString(),
            row.posting.Direction == EntryDirection.Credit ? "credito" : "debito",
            row.posting.Amount,
            row.journal.Reference ?? row.journal.Kind.ToString())).ToList();
    }

    private static string AccountNumber(int existing)
    {
        var serial = (1000001 + existing).ToString();
        var sum = 0;
        for (var i = 0; i < serial.Length; i++)
            sum += (serial[i] - '0') * (i + 2);
        return serial + (sum % 10);
    }

    private async Task AddLedgerIfMissing(Guid id, string kind, NormalBalance normal, bool guard, CancellationToken cancellationToken)
    {
        if (!await db.LedgerAccounts.AnyAsync(item => item.Id == id, cancellationToken))
            db.LedgerAccounts.Add(NewLedger(id, kind, normal, guard));
    }

    private static LedgerAccount NewLedger(Guid id, string kind, NormalBalance normal, bool guard) =>
        new() { Id = id, Kind = kind, Normal = normal, GuardNonNegative = guard };

    private static Boleto Catalog(string name, decimal amount, DateOnly due) => new()
    {
        Id = Guid.NewGuid(),
        Line = "VB" + Guid.NewGuid().ToString("N")[..20],
        Beneficiary = name,
        Amount = amount,
        DueDate = due,
        Status = BoletoStatus.Open,
        External = true
    };

    private static BoletoView MapBoleto(Boleto boleto, Guid userId) => new(
        boleto.Id, boleto.Line, boleto.Beneficiary, boleto.Amount, boleto.DueDate,
        boleto.Status == BoletoStatus.Paid ? "pago" : "aberto", boleto.External, boleto.PayeeCustomerId == userId);

    private static ReceiptView ToReceipt(JournalEntry entry, decimal amount) => new(
        entry.Id, entry.BusinessDate, entry.Kind.ToString(), amount, entry.Reference ?? entry.Kind.ToString(),
        "AUT-" + entry.Id.ToString("N")[..10].ToUpperInvariant());

    private static string Display(string number) => number.Length < 2 ? number : number[..^1] + "-" + number[^1];

    private static string Pan(string seed)
    {
        var digits = "99990000" + seed;
        return digits.Length >= 16 ? digits[..16] : digits.PadRight(16, '0');
    }

    private static string NormalizeAccount(string number)
    {
        var digits = new string(number.Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
            throw new BankingException("unknown_account", "Informe a conta com 7 dígitos e o dígito.");
        return digits;
    }

    private static string NormalizeKey(string value) => value.Trim();

    private static (PixKeyKind Kind, string Value) ParseKey(string kind, string value, string cpf)
    {
        var normalized = kind.Trim().ToLowerInvariant();
        if (normalized is "cpf")
        {
            var digits = Cpf.Digits(value);
            if (digits != cpf)
                throw new BankingException("cpf_invalid", "A chave CPF precisa ser a do titular.");
            return (PixKeyKind.Cpf, digits);
        }
        if (normalized is "email")
        {
            var email = value.Trim().ToLowerInvariant();
            if (!email.Contains('@'))
                throw new BankingException("pix_key_invalid", "E-mail inválido.");
            return (PixKeyKind.Email, email);
        }
        if (normalized is "celular" or "phone")
        {
            var phone = new string(value.Where(char.IsDigit).ToArray());
            if (phone.Length is < 10 or > 13)
                throw new BankingException("pix_key_invalid", "Celular inválido.");
            return (PixKeyKind.Phone, phone);
        }
        if (normalized is "aleatoria" or "random")
            return (PixKeyKind.Random, Guid.NewGuid().ToString());
        throw new BankingException("pix_key_invalid", "Tipo de chave desconhecido.");
    }

    private static bool IsUnique(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgres && postgres.SqlState == PostgresErrorCodes.UniqueViolation;

    private sealed class Noop : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RedisRelease(IDatabase database, string key) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await database.KeyDeleteAsync(key);
    }
}

public static class DemoBook
{
    public sealed record Person(Guid Id, string Name, string Cpf);
    public static readonly Person Ana = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "Ana Ribeiro", "39053344705");
    public static readonly Person Bruno = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "Bruno Lima", "52998224725");
    public static readonly Person Carla = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), "Carla Mendes", "11144477735");
}
