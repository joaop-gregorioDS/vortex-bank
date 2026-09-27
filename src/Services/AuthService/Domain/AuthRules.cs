namespace Bankcore.Auth.Domain;

public static class AuthRules
{
    public static void RequireEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal) || email.Length > 200)
            throw new AuthException("email_invalid", "Informe um e-mail válido.");
    }

    public static void RequirePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new AuthException("password_short", "A senha precisa ter pelo menos 8 caracteres.");
    }

    public static void RequireName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 3)
            throw new AuthException("name_invalid", "Informe o nome completo.");
    }
}

public sealed class AuthException : Exception
{
    public AuthException(string code, string message) : base(message) => Code = code;

    public string Code { get; }
}
