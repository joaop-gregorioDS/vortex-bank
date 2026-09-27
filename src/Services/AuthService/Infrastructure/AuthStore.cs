using System.Security.Cryptography;
using System.Text;
using Bankcore.Auth.Application;
using Bankcore.Auth.Domain;
using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Bankcore.Auth.Infrastructure;

public sealed class AuthUser
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Cpf { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class RefreshSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? ReplacedById { get; set; }
}

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<AuthUser> Users => Set<AuthUser>();
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthUser>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => user.Cpf).IsUnique();
            entity.Property(user => user.Email).HasMaxLength(200);
            entity.Property(user => user.Name).HasMaxLength(160);
            entity.Property(user => user.Cpf).HasMaxLength(11);
        });

        modelBuilder.Entity<RefreshSession>(entity =>
        {
            entity.ToTable("refresh_sessions");
            entity.HasKey(session => session.Id);
            entity.HasIndex(session => session.TokenHash).IsUnique();
            entity.Property(session => session.TokenHash).HasMaxLength(64);
        });
    }
}

public sealed class AuthStore(AuthDbContext db, SigningKeys keys) : IAuthService
{
    private static readonly TimeSpan AccessLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(14);

    public async Task<AuthSession> RegisterAsync(string name, string email, string cpf, string password, CancellationToken cancellationToken)
    {
        AuthRules.RequireName(name);
        AuthRules.RequireEmail(email);
        AuthRules.RequirePassword(password);
        var digits = CpfCheck.Digits(cpf);
        var normalized = email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == normalized || user.Cpf == digits, cancellationToken))
            throw new AuthException("already_registered", "Já existe um cadastro com este e-mail ou CPF.");

        var user = new AuthUser
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = normalized,
            Cpf = digits,
            PasswordHash = Argon2.Hash(password, 3, 65536, 1, Argon2Type.HybridAddressing, 32),
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);
        return await IssueAsync(user, Guid.NewGuid(), cancellationToken);
    }

    public async Task<AuthSession> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(item => item.Email == normalized, cancellationToken);
        if (user is null || !Argon2.Verify(user.PasswordHash, password))
            throw new AuthException("invalid_credentials", "E-mail ou senha incorretos.");
        return await IssueAsync(user, Guid.NewGuid(), cancellationToken);
    }

    public async Task<AuthSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = Hash(refreshToken);
        var current = await db.RefreshSessions.SingleOrDefaultAsync(session => session.TokenHash == hash, cancellationToken);
        if (current is null)
            throw new AuthException("refresh_invalid", "Sessão expirada. Entre de novo.");

        if (current.RevokedAtUtc is not null || current.ReplacedById is not null)
        {
            await RevokeFamilyAsync(current.FamilyId, cancellationToken);
            throw new AuthException("refresh_reused", "Esta sessão foi revogada. Entre de novo.");
        }

        if (current.ExpiresAtUtc < DateTime.UtcNow)
            throw new AuthException("refresh_invalid", "Sessão expirada. Entre de novo.");

        var user = await db.Users.SingleAsync(item => item.Id == current.UserId, cancellationToken);
        current.RevokedAtUtc = DateTime.UtcNow;
        var session = await IssueAsync(user, current.FamilyId, cancellationToken);
        current.ReplacedById = await db.RefreshSessions
            .Where(item => item.TokenHash == Hash(session.RefreshToken))
            .Select(item => item.Id)
            .SingleAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = Hash(refreshToken);
        var current = await db.RefreshSessions.SingleOrDefaultAsync(session => session.TokenHash == hash, cancellationToken);
        if (current is null)
            return;
        await RevokeFamilyAsync(current.FamilyId, cancellationToken);
    }

    public async Task<ProfileView?> ProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        return user is null ? null : new ProfileView(user.Id, user.Name, user.Email, user.Cpf);
    }

    public static async Task SeedDemoAsync(AuthDbContext db, CancellationToken cancellationToken)
    {
        foreach (var person in DemoPeople.All)
        {
            if (await db.Users.AnyAsync(user => user.Id == person.Id, cancellationToken))
                continue;
            db.Users.Add(new AuthUser
            {
                Id = person.Id,
                Name = person.Name,
                Email = person.Email,
                Cpf = person.Cpf,
                PasswordHash = Argon2.Hash(person.Password, 3, 65536, 1, Argon2Type.HybridAddressing, 32),
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthSession> IssueAsync(AuthUser user, Guid familyId, CancellationToken cancellationToken)
    {
        var refresh = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var refreshExpires = DateTime.UtcNow.Add(RefreshLifetime);
        db.RefreshSessions.Add(new RefreshSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = Hash(refresh),
            ExpiresAtUtc = refreshExpires
        });
        await db.SaveChangesAsync(cancellationToken);

        var accessExpires = DateTime.UtcNow.Add(AccessLifetime);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = SigningKeys.Issuer,
            Audience = SigningKeys.Audience,
            Expires = accessExpires,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("name", user.Name)
            ]),
            SigningCredentials = new SigningCredentials(keys.PrivateKey, SecurityAlgorithms.EcdsaSha256)
        };
        var handler = new JwtSecurityTokenHandler();
        var access = handler.WriteToken(handler.CreateToken(descriptor));
        return new AuthSession(user.Id, user.Name, user.Email, user.Cpf, access, accessExpires, refresh, refreshExpires);
    }

    private async Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var sessions = await db.RefreshSessions.Where(session => session.FamilyId == familyId && session.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var session in sessions)
            session.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}

public static class DemoPeople
{
    public sealed record Person(Guid Id, string Name, string Email, string Cpf, string Password);

    public static readonly Person Ana = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "Ana Ribeiro", "ana.ribeiro@vortexbank.demo", "39053344705", "Ana-demo-2026");
    public static readonly Person Bruno = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "Bruno Lima", "bruno.lima@vortexbank.demo", "52998224725", "Bruno-demo-2026");
    public static readonly Person Carla = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), "Carla Mendes", "carla.mendes@vortexbank.demo", "11144477735", "Carla-demo-2026");
    public static IReadOnlyList<Person> All { get; } = [Ana, Bruno, Carla];
}
