using Bankcore.Transactions.Domain;

namespace Bankcore.Transactions.Domain.Tests;

public class LedgerTests
{
    private static readonly Guid Cash = Guid.NewGuid();
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Bruno = Guid.NewGuid();
    private static readonly Guid Fee = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 9, 28);

    [Fact]
    public void Pix_moves_between_customers_and_does_not_touch_cash()
    {
        var journal = Operations.Pix(Ana, Bruno, 100m, 2_500m, 0m, Day);
        Assert.Equal(0m, journal.Delta(Cash, NormalBalance.Debit));
        Assert.Equal(-100m, journal.Delta(Ana, NormalBalance.Credit));
        Assert.Equal(100m, journal.Delta(Bruno, NormalBalance.Credit));
        Assert.Equal(journal.Postings.Where(p => p.Direction == EntryDirection.Credit).Sum(p => p.Amount),
            journal.Postings.Where(p => p.Direction == EntryDirection.Debit).Sum(p => p.Amount));
    }

    [Fact]
    public void Pix_rejects_insufficient_funds_and_daily_limit()
    {
        Assert.Throws<BankingException>(() => Operations.Pix(Ana, Bruno, 10m, 5m, 0m, Day));
        Assert.Throws<BankingException>(() => Operations.Pix(Ana, Bruno, 1m, 100m, 20_000m, Day));
        Assert.Throws<BankingException>(() => Operations.Pix(Ana, Ana, 1m, 100m, 0m, Day));
    }

    [Fact]
    public void Ted_fee_is_separate_from_principal()
    {
        var fee = Operations.TedFeeOnly(Ana, Fee, 100m, 50m, Day);
        Assert.Equal(-Operations.TedFee, fee.Delta(Ana, NormalBalance.Credit));
        Assert.Equal(Operations.TedFee, fee.Delta(Fee, NormalBalance.Credit));

        var settled = Operations.TedSettlement(Ana, Bruno, 50m, 91.10m, Day.AddDays(1));
        Assert.Equal(-50m, settled.Delta(Ana, NormalBalance.Credit));
        Assert.Equal(50m, settled.Delta(Bruno, NormalBalance.Credit));
        Assert.DoesNotContain(settled.Postings, p => p.LedgerAccountId == Fee);
    }

    [Fact]
    public void External_boleto_reduces_cash_and_customer_boleto_does_not()
    {
        var external = Operations.ExternalBoleto(Ana, Cash, 80m, 100m, Day);
        Assert.Equal(-80m, external.Delta(Ana, NormalBalance.Credit));
        Assert.Equal(-80m, external.Delta(Cash, NormalBalance.Debit));

        var internalBoleto = Operations.CustomerBoleto(Ana, Bruno, 80m, 100m, Day);
        Assert.Equal(0m, internalBoleto.Delta(Cash, NormalBalance.Debit));
        Assert.Equal(80m, internalBoleto.Delta(Bruno, NormalBalance.Credit));
    }

    [Fact]
    public void Yield_is_at_least_one_cent_and_null_when_balance_is_zero()
    {
        Assert.Null(Operations.SavingsYield(Guid.NewGuid(), Ana, 0m, Day));
        var yield = Operations.SavingsYield(Guid.NewGuid(), Ana, 0.50m, Day);
        Assert.NotNull(yield);
        Assert.Equal(0.01m, yield!.Delta(Ana, NormalBalance.Credit));
    }

    [Fact]
    public void Credit_purchase_checks_limit_and_invoice_pays_receivable()
    {
        var receivable = Guid.NewGuid();
        Assert.Throws<BankingException>(() => Operations.CreditPurchase(receivable, Cash, 100m, 4_950m, Day));
        var purchase = Operations.CreditPurchase(receivable, Cash, 200m, 0m, Day);
        Assert.Equal(200m, purchase.Delta(receivable, NormalBalance.Debit));
        Assert.Equal(-200m, purchase.Delta(Cash, NormalBalance.Debit));

        var payment = Operations.PayInvoice(Ana, receivable, 200m, 2_000m, 200m, Day);
        Assert.Equal(-200m, payment.Delta(Ana, NormalBalance.Credit));
        Assert.Equal(-200m, payment.Delta(receivable, NormalBalance.Debit));
    }

    [Fact]
    public void Clock_skips_weekend()
    {
        Assert.Equal(new DateOnly(2026, 9, 28), Operations.NextBusinessDay(new DateOnly(2026, 9, 25)));
        Assert.Equal(new DateOnly(2026, 9, 29), Operations.NextBusinessDay(new DateOnly(2026, 9, 28)));
    }

    [Fact]
    public void Rejects_more_than_two_decimal_places()
    {
        Assert.Throws<BankingException>(() => Money.Of(10.001m));
        Assert.Throws<BankingException>(() => Money.Of(0m));
    }
}
