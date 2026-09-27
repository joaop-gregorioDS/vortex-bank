namespace Bankcore.Transactions.Domain;

public static class Operations
{
    public const decimal TedFee = 8.90m;
    public const decimal PixDailyLimit = 20_000m;
    public const decimal OpeningBalance = 2_500m;
    public const decimal CreditLimit = 5_000m;
    public const decimal AnnualSavingsRate = 0.1065m;
    public const int BusinessDaysInYear = 252;

    public static JournalDraft OpeningDeposit(Guid cashId, Guid checkingId, DateOnly date, decimal amount)
    {
        var money = Money.Of(amount);
        return JournalDraft.Create(
            JournalKind.Seed,
            date,
            new PostingDraft(cashId, money.Amount, EntryDirection.Debit),
            new PostingDraft(checkingId, money.Amount, EntryDirection.Credit));
    }

    public static JournalDraft Pix(
        Guid fromChecking,
        Guid toChecking,
        decimal amount,
        decimal fromBalance,
        decimal sentToday,
        DateOnly date)
    {
        var money = Money.Of(amount);
        if (fromChecking == toChecking)
            throw new BankingException("self_transfer", "Não é possível enviar para a própria conta.");
        if (sentToday + money.Amount > PixDailyLimit)
            throw new BankingException("pix_limit", "O limite diário de PIX é R$ 20.000,00.");
        EnsureFunds(fromBalance, money.Amount);
        return JournalDraft.Create(
            JournalKind.Pix,
            date,
            new PostingDraft(fromChecking, money.Amount, EntryDirection.Debit),
            new PostingDraft(toChecking, money.Amount, EntryDirection.Credit));
    }

    public static JournalDraft TedFeeOnly(Guid checkingId, Guid feeIncomeId, decimal balance, decimal amount, DateOnly date)
    {
        Money.Of(amount);
        EnsureFunds(balance, amount + TedFee);
        return JournalDraft.Create(
            JournalKind.TedFee,
            date,
            new PostingDraft(checkingId, TedFee, EntryDirection.Debit),
            new PostingDraft(feeIncomeId, TedFee, EntryDirection.Credit));
    }

    public static JournalDraft TedSettlement(Guid fromChecking, Guid toChecking, decimal amount, decimal fromBalance, DateOnly date)
    {
        var money = Money.Of(amount);
        if (fromChecking == toChecking)
            throw new BankingException("self_transfer", "Não é possível transferir para a própria conta.");
        EnsureFunds(fromBalance, money.Amount);
        return JournalDraft.Create(
            JournalKind.Ted,
            date,
            new PostingDraft(fromChecking, money.Amount, EntryDirection.Debit),
            new PostingDraft(toChecking, money.Amount, EntryDirection.Credit));
    }

    public static JournalDraft ReverseTedFee(Guid checkingId, Guid feeIncomeId, DateOnly date) =>
        JournalDraft.Create(
            JournalKind.TedFeeReversal,
            date,
            new PostingDraft(feeIncomeId, TedFee, EntryDirection.Debit),
            new PostingDraft(checkingId, TedFee, EntryDirection.Credit));

    public static JournalDraft ExternalBoleto(Guid checkingId, Guid cashId, decimal amount, decimal balance, DateOnly date)
    {
        var money = Money.Of(amount);
        EnsureFunds(balance, money.Amount);
        return JournalDraft.Create(
            JournalKind.Boleto,
            date,
            new PostingDraft(checkingId, money.Amount, EntryDirection.Debit),
            new PostingDraft(cashId, money.Amount, EntryDirection.Credit));
    }

    public static JournalDraft CustomerBoleto(Guid payerChecking, Guid payeeChecking, decimal amount, decimal payerBalance, DateOnly date)
    {
        var money = Money.Of(amount);
        if (payerChecking == payeeChecking)
            throw new BankingException("self_transfer", "Não é possível pagar o próprio boleto.");
        EnsureFunds(payerBalance, money.Amount);
        return JournalDraft.Create(
            JournalKind.Boleto,
            date,
            new PostingDraft(payerChecking, money.Amount, EntryDirection.Debit),
            new PostingDraft(payeeChecking, money.Amount, EntryDirection.Credit));
    }

    public static JournalDraft SavingsMove(Guid fromId, Guid toId, decimal amount, decimal fromBalance, DateOnly date)
    {
        var money = Money.Of(amount);
        if (fromId == toId)
            throw new BankingException("self_transfer", "Escolha a outra conta.");
        EnsureFunds(fromBalance, money.Amount);
        return JournalDraft.Create(
            JournalKind.SavingsTransfer,
            date,
            new PostingDraft(fromId, money.Amount, EntryDirection.Debit),
            new PostingDraft(toId, money.Amount, EntryDirection.Credit));
    }

    public static JournalDraft? SavingsYield(Guid expenseId, Guid savingsId, decimal savingsBalance, DateOnly date)
    {
        if (savingsBalance <= 0)
            return null;
        var daily = savingsBalance * AnnualSavingsRate / BusinessDaysInYear;
        var amount = Money.RoundYield(daily);
        if (amount <= 0)
            return null;
        return JournalDraft.Create(
            JournalKind.SavingsYield,
            date,
            new PostingDraft(expenseId, amount, EntryDirection.Debit),
            new PostingDraft(savingsId, amount, EntryDirection.Credit));
    }

    public static JournalDraft DebitPurchase(Guid checkingId, Guid cashId, decimal amount, decimal balance, DateOnly date)
    {
        var money = Money.Of(amount);
        EnsureFunds(balance, money.Amount);
        return JournalDraft.Create(
            JournalKind.DebitPurchase,
            date,
            new PostingDraft(checkingId, money.Amount, EntryDirection.Debit),
            new PostingDraft(cashId, money.Amount, EntryDirection.Credit));
    }

    public static JournalDraft CreditPurchase(Guid receivableId, Guid cashId, decimal amount, decimal used, DateOnly date)
    {
        var money = Money.Of(amount);
        if (used + money.Amount > CreditLimit)
            throw new BankingException("credit_limit", "A compra passa do limite do cartão.");
        return JournalDraft.Create(
            JournalKind.CreditPurchase,
            date,
            new PostingDraft(receivableId, money.Amount, EntryDirection.Debit),
            new PostingDraft(cashId, money.Amount, EntryDirection.Credit));
    }

    public static JournalDraft PayInvoice(Guid checkingId, Guid receivableId, decimal amount, decimal checkingBalance, decimal receivableBalance, DateOnly date)
    {
        var money = Money.Of(amount);
        if (money.Amount > receivableBalance)
            throw new BankingException("invoice_amount", "A fatura é maior que o saldo do cartão.");
        EnsureFunds(checkingBalance, money.Amount);
        return JournalDraft.Create(
            JournalKind.InvoicePayment,
            date,
            new PostingDraft(checkingId, money.Amount, EntryDirection.Debit),
            new PostingDraft(receivableId, money.Amount, EntryDirection.Credit));
    }

    public static DateOnly NextBusinessDay(DateOnly date)
    {
        var next = date.AddDays(1);
        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            next = next.AddDays(1);
        return next;
    }

    private static void EnsureFunds(decimal balance, decimal needed)
    {
        if (balance < needed)
            throw new BankingException("insufficient_funds", "Saldo insuficiente.");
    }
}
