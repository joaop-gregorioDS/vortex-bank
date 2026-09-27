using System.Security.Claims;
using Bankcore.Auth.Application;
using Bankcore.Auth.Domain;
using Bankcore.Auth.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("AuthDb")
    ?? builder.Configuration.GetConnectionString("Auth")
    ?? "Host=localhost;Port=5432;Database=bankcore_auth;Username=bankcore;Password=bankcore-dev";
builder.Services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connection));

var keyDir = builder.Configuration["KEY_DIR"]
    ?? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "..", ".keys"));
var keys = SigningKeys.Load(keyDir);
builder.Services.AddSingleton(keys);
builder.Services.AddScoped<IAuthService, AuthStore>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = SigningKeys.Issuer,
        ValidateAudience = true,
        ValidAudience = SigningKeys.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = keys.PrivateKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = "name",
        RoleClaimType = "role"
    };
});
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Bankcore Auth", Version = "v1", Description = "Autenticação do simulador Vortex Bank. Nenhum valor é real." });
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Access token de um cliente de demonstração."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();
    await AuthStore.SeedDemoAsync(db, CancellationToken.None);
}

var pathBase = builder.Configuration["Swagger:PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
    app.UsePathBase(pathBase);
app.UseSwagger();
app.UseSwaggerUI(ui =>
{
    ui.RoutePrefix = string.IsNullOrWhiteSpace(pathBase) ? "swagger" : string.Empty;
    ui.SwaggerEndpoint(string.IsNullOrWhiteSpace(pathBase) ? "/swagger/v1/swagger.json" : "swagger/v1/swagger.json", "Bankcore Auth");
});

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (AuthException exception)
    {
        context.Response.StatusCode = exception.Code is "invalid_credentials" or "refresh_invalid" or "refresh_reused" ? StatusCodes.Status401Unauthorized : StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { code = exception.Code, message = exception.Message });
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPost("/register", async (RegisterBody body, IAuthService auth, HttpContext http, CancellationToken ct) =>
    Results.Ok(await WriteSession(http, await auth.RegisterAsync(body.Name, body.Email, body.Cpf, body.Password, ct))));
app.MapPost("/login", async (LoginBody body, IAuthService auth, HttpContext http, CancellationToken ct) =>
    Results.Ok(await WriteSession(http, await auth.LoginAsync(body.Email, body.Password, ct))));
app.MapPost("/refresh", async (HttpContext http, IAuthService auth, CancellationToken ct) =>
{
    if (!http.Request.Cookies.TryGetValue("bankcore_refresh", out var refresh) || string.IsNullOrWhiteSpace(refresh))
        return Results.Unauthorized();
    return Results.Ok(await WriteSession(http, await auth.RefreshAsync(refresh, ct)));
});
app.MapPost("/logout", async (HttpContext http, IAuthService auth, CancellationToken ct) =>
{
    if (http.Request.Cookies.TryGetValue("bankcore_refresh", out var refresh))
        await auth.RevokeAsync(refresh, ct);
    http.Response.Cookies.Delete("bankcore_refresh", Cookie(http));
    return Results.NoContent();
});
app.MapGet("/me", async (ClaimsPrincipal principal, IAuthService auth, CancellationToken ct) =>
{
    var id = Guid.Parse(principal.FindFirst("sub")!.Value);
    var profile = await auth.ProfileAsync(id, ct);
    return profile is null ? Results.NotFound() : Results.Ok(profile);
}).RequireAuthorization();

app.Run();

static async Task<SessionBody> WriteSession(HttpContext http, AuthSession session)
{
    http.Response.Cookies.Append("bankcore_refresh", session.RefreshToken, Cookie(http, session.RefreshExpiresUtc));
    return new SessionBody(session.UserId, session.Name, session.Email, session.Cpf, session.AccessToken, session.AccessExpiresUtc);
}

static CookieOptions Cookie(HttpContext http, DateTime? expires = null)
{
    var secure = http.Request.IsHttps || string.Equals(http.Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);
    return new CookieOptions
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Lax,
        Path = "/api/auth",
        Expires = expires
    };
}

internal sealed record RegisterBody(string Name, string Email, string Cpf, string Password);
internal sealed record LoginBody(string Email, string Password);
internal sealed record SessionBody(Guid UserId, string Name, string Email, string Cpf, string AccessToken, DateTime AccessExpiresUtc);
