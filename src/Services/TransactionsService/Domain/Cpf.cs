namespace Bankcore.Transactions.Domain;

public static class Cpf
{
    public static string Digits(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 11 || digits.Distinct().Count() == 1)
            throw new BankingException("cpf_invalid", "CPF inválido.");

        var numbers = digits.Select(c => c - '0').ToArray();
        if (CheckDigit(numbers, 9) != numbers[9] || CheckDigit(numbers, 10) != numbers[10])
            throw new BankingException("cpf_invalid", "CPF inválido.");
        return digits;
    }

    private static int CheckDigit(int[] numbers, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
            sum += numbers[i] * (length + 1 - i);
        var digit = (sum * 10) % 11;
        return digit == 10 ? 0 : digit;
    }
}
