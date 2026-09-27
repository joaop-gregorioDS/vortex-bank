namespace Bankcore.Transactions.Application;

public sealed record AccountView(Guid Id, string Kind, string Agency, string Number, decimal Balance);
public sealed record CardView(Guid Id, string Kind, string Pan, string Holder, string Expiry, string Cvv, decimal? Limit, decimal? Used, Guid? OpenInvoiceId, decimal? OpenInvoiceAmount, DateOnly? OpenInvoiceDue);
public sealed record StatementLine(Guid JournalId, DateOnly BusinessDate, DateTime CreatedAt, string Kind, string Direction, decimal Amount, string Description);
public sealed record PixKeyView(Guid Id, string Kind, string Value);
public sealed record BoletoView(Guid Id, string Line, string Beneficiary, decimal Amount, DateOnly DueDate, string Status, bool External, bool Mine);
public sealed record TedView(Guid Id, decimal Amount, decimal Fee, string Status, string Agency, string Number, DateOnly ScheduledFor);
public sealed record ReceiptView(Guid JournalId, DateOnly BusinessDate, string Kind, decimal Amount, string Description, string Authentication);
public sealed record HomeView(DateOnly BusinessDate, IReadOnlyList<AccountView> Accounts, IReadOnlyList<CardView> Cards, IReadOnlyList<PixKeyView> PixKeys, IReadOnlyList<BoletoView> Boletos, IReadOnlyList<TedView> Teds, IReadOnlyList<StatementLine> Recent);
public sealed record ClockView(DateOnly BusinessDate);

public interface IBank
{
    Task<HomeView> HomeAsync(Guid userId, CancellationToken cancellationToken);
    Task ProvisionAsync(Guid userId, string name, string cpf, CancellationToken cancellationToken);
    Task<IReadOnlyList<StatementLine>> StatementAsync(Guid userId, Guid accountId, CancellationToken cancellationToken);
    Task<ReceiptView> ReceiptAsync(Guid userId, Guid journalId, CancellationToken cancellationToken);
    Task<PixKeyView> AddPixKeyAsync(Guid userId, string kind, string value, CancellationToken cancellationToken);
    Task<ReceiptView> SendPixAsync(Guid userId, string key, decimal amount, string idempotencyKey, string correlationId, string? ip, CancellationToken cancellationToken);
    Task<TedView> ScheduleTedAsync(Guid userId, string agency, string number, decimal amount, string idempotencyKey, string correlationId, string? ip, CancellationToken cancellationToken);
    Task<ReceiptView> MoveSavingsAsync(Guid userId, string direction, decimal amount, string idempotencyKey, string correlationId, string? ip, CancellationToken cancellationToken);
    Task<BoletoView> IssueBoletoAsync(Guid userId, decimal amount, DateOnly dueDate, string description, CancellationToken cancellationToken);
    Task<ReceiptView> PayBoletoAsync(Guid userId, string line, string idempotencyKey, string correlationId, string? ip, CancellationToken cancellationToken);
    Task<ReceiptView> BuyAsync(Guid userId, Guid cardId, string merchant, decimal amount, string idempotencyKey, string correlationId, string? ip, CancellationToken cancellationToken);
    Task<ReceiptView> PayInvoiceAsync(Guid userId, Guid invoiceId, string idempotencyKey, string correlationId, string? ip, CancellationToken cancellationToken);
    Task<ClockView> AdvanceAsync(Guid userId, string correlationId, string? ip, CancellationToken cancellationToken);
}
