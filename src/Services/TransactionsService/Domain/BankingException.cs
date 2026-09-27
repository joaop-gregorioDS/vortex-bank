namespace Bankcore.Transactions.Domain;

public sealed class BankingException : Exception
{
    public BankingException(string code, string message) : base(message) => Code = code;

    public string Code { get; }
}
