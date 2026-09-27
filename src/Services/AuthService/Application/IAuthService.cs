namespace Bankcore.Auth.Application;

public sealed record AuthSession(Guid UserId, string Name, string Email, string Cpf, string AccessToken, DateTime AccessExpiresUtc, string RefreshToken, DateTime RefreshExpiresUtc);

public sealed record ProfileView(Guid UserId, string Name, string Email, string Cpf);

public interface IAuthService
{
    Task<AuthSession> RegisterAsync(string name, string email, string cpf, string password, CancellationToken cancellationToken);
    Task<AuthSession> LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task<AuthSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);
    Task<ProfileView?> ProfileAsync(Guid userId, CancellationToken cancellationToken);
}
