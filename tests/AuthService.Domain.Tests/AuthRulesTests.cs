using Bankcore.Auth.Domain;

namespace Bankcore.Auth.Domain.Tests;

public class AuthRulesTests
{
    [Fact]
    public void Rejects_short_password_and_accepts_a_real_email()
    {
        Assert.Throws<AuthException>(() => AuthRules.RequirePassword("curta"));
        AuthRules.RequirePassword("Ana-demo-2026");
        AuthRules.RequireEmail("ana.ribeiro@vortexbank.demo");
        Assert.Throws<AuthException>(() => AuthRules.RequireEmail("sem-arroba"));
    }
}
