namespace Bankcore.Transactions.Domain;

public readonly record struct Money
{
    public decimal Amount { get; }

    private Money(decimal amount) => Amount = amount;

    public static Money Of(decimal amount)
    {
        if (amount <= 0)
            throw new BankingException("amount_not_positive", "O valor precisa ser positivo.");
        if (amount != decimal.Round(amount, 2, MidpointRounding.ToZero))
            throw new BankingException("amount_scale", "Use no máximo duas casas decimais.");
        return new Money(amount);
    }

    public static decimal RoundYield(decimal raw)
    {
        var rounded = decimal.Round(raw, 2, MidpointRounding.ToZero);
        if (rounded <= 0 && raw > 0)
            return 0.01m;
        return rounded;
    }
}
