namespace Bankcore.Transactions.Domain;

public enum EntryDirection
{
    Debit = 1,
    Credit = 2
}

public enum JournalKind
{
    Seed = 1,
    Pix = 2,
    Ted = 3,
    TedFee = 4,
    TedFeeReversal = 5,
    Boleto = 6,
    SavingsTransfer = 7,
    SavingsYield = 8,
    DebitPurchase = 9,
    CreditPurchase = 10,
    InvoicePayment = 11
}

public enum NormalBalance
{
    Credit,
    Debit
}

public sealed record PostingDraft(Guid LedgerAccountId, decimal Amount, EntryDirection Direction);

public sealed class JournalDraft
{
    private JournalDraft(JournalKind kind, DateOnly businessDate, IReadOnlyList<PostingDraft> postings)
    {
        Kind = kind;
        BusinessDate = businessDate;
        Postings = postings;
    }

    public JournalKind Kind { get; }
    public DateOnly BusinessDate { get; }
    public IReadOnlyList<PostingDraft> Postings { get; }

    public static JournalDraft Create(JournalKind kind, DateOnly businessDate, params PostingDraft[] postings)
    {
        if (postings.Length < 2)
            throw new BankingException("unbalanced", "Um lançamento precisa de pelo menos duas partidas.");

        decimal credits = 0;
        decimal debits = 0;
        foreach (var posting in postings)
        {
            if (posting.Amount <= 0 || posting.Amount != decimal.Round(posting.Amount, 2))
                throw new BankingException("amount_scale", "Partida com valor inválido.");
            if (posting.Direction == EntryDirection.Credit)
                credits += posting.Amount;
            else
                debits += posting.Amount;
        }

        if (credits != debits)
            throw new BankingException("unbalanced", "Créditos e débitos não fecham.");

        return new JournalDraft(kind, businessDate, postings);
    }

    public decimal Delta(Guid ledgerAccountId, NormalBalance normal)
    {
        decimal delta = 0;
        foreach (var posting in Postings)
        {
            if (posting.LedgerAccountId != ledgerAccountId)
                continue;
            var creditIncreases = normal == NormalBalance.Credit;
            var sign = posting.Direction == EntryDirection.Credit ? 1m : -1m;
            if (!creditIncreases)
                sign = -sign;
            delta += sign * posting.Amount;
        }

        return delta;
    }
}
