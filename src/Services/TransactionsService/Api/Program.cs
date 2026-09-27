using System.Security.Claims;
using System.Security.Cryptography;
using Bankcore.Transactions.Application;
using Bankcore.Transactions.Domain;
using Bankcore.Transactions.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("TransDb")
    ?? builder.Configuration.GetConnectionString("Bank")
    ?? "Host=localhost;Port=5432;Database=bankcore_trans;Username=bankcore;Password=bankcore-dev";
builder.Services.AddDbContext<BankDbContext>(options => options.UseNpgsql(connection));

var keyDir = builder.Configuration["KEY_DIR"]
    ?? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "..", ".keys"));
var publicPath = builder.Configuration["Auth:PublicKeyPath"] ?? Path.Combine(keyDir, "auth-public.pem");
var deadline = DateTime.UtcNow.AddSeconds(30);
while (!File.Exists(publicPath) && DateTime.UtcNow < deadline)
    Thread.Sleep(200);
if (!File.Exists(publicPath))
    throw new InvalidOperationException("Chave pública do auth-api não encontrada. Suba o serviço de autenticação primeiro.");

var ecdsa = ECDsa.Create();
ecdsa.ImportFromPem(File.ReadAllText(publicPath));
var signingKey = new ECDsaSecurityKey(ecdsa);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = "bankcore-auth",
        ValidateAudience = true,
        ValidAudience = "bankcore",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();

var redisUrl = builder.Configuration["Redis:ConnectionString"] ?? builder.Configuration["Redis"];
if (!string.IsNullOrWhiteSpace(redisUrl))
{
    var redisOptions = ConfigurationOptions.Parse(redisUrl);
    redisOptions.AbortOnConnectFail = false;
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
}
else
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => null!);
}

builder.Services.AddScoped<IBank, BankService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Bankcore Transactions", Version = "v1", Description = "Ledger simulado do Vortex Bank. Nenhum valor é real." });
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Access token de um cliente de demonstração. Sem token, POST de dinheiro responde 401."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
    await BankSchema.EnsureAsync(db, CancellationToken.None);
    await BankService.SeedAsync(db, CancellationToken.None);
}

var pathBase = builder.Configuration["Swagger:PathBase"];
if (!string.IsNullOrWhiteSpace(pathBase))
    app.UsePathBase(pathBase);
app.UseSwagger();
app.UseSwaggerUI(ui =>
{
    ui.RoutePrefix = string.IsNullOrWhiteSpace(pathBase) ? "swagger" : string.Empty;
    ui.SwaggerEndpoint(string.IsNullOrWhiteSpace(pathBase) ? "/swagger/v1/swagger.json" : "swagger/v1/swagger.json", "Bankcore Transactions");
});

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (BankingException exception)
    {
        context.Response.StatusCode = exception.Code is "not_found" ? StatusCodes.Status404NotFound : StatusCodes.Status422UnprocessableEntity;
        await context.Response.WriteAsJsonAsync(new { code = exception.Code, message = exception.Message });
    }
});

app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var api = app.MapGroup("").RequireAuthorization();
api.MapPost("/me/provision", async (ProvisionBody body, ClaimsPrincipal user, IBank bank, CancellationToken ct) =>
{
    await bank.ProvisionAsync(UserId(user), body.Name, body.Cpf, ct);
    return Results.NoContent();
});
api.MapGet("/home", async (ClaimsPrincipal user, IBank bank, CancellationToken ct) => Results.Ok(await bank.HomeAsync(UserId(user), ct)));
api.MapGet("/statement", async (Guid accountId, ClaimsPrincipal user, IBank bank, CancellationToken ct) => Results.Ok(await bank.StatementAsync(UserId(user), accountId, ct)));
api.MapGet("/receipts/{journalId:guid}", async (Guid journalId, ClaimsPrincipal user, IBank bank, CancellationToken ct) => Results.Ok(await bank.ReceiptAsync(UserId(user), journalId, ct)));
api.MapPost("/pix/keys", async (PixKeyBody body, ClaimsPrincipal user, IBank bank, CancellationToken ct) => Results.Ok(await bank.AddPixKeyAsync(UserId(user), body.Kind, body.Value, ct)));
api.MapPost("/pix", async (PixBody body, ClaimsPrincipal user, IBank bank, HttpContext http, CancellationToken ct) => Results.Ok(await bank.SendPixAsync(UserId(user), body.Key, body.Amount, Key(http), Correlation(http), Ip(http), ct)));
api.MapPost("/ted", async (TedBody body, ClaimsPrincipal user, IBank bank, HttpContext http, CancellationToken ct) => Results.Ok(await bank.ScheduleTedAsync(UserId(user), body.Agency, body.Number, body.Amount, Key(http), Correlation(http), Ip(http), ct)));
api.MapPost("/savings", async (SavingsBody body, ClaimsPrincipal user, IBank bank, HttpContext http, CancellationToken ct) => Results.Ok(await bank.MoveSavingsAsync(UserId(user), body.Direction, body.Amount, Key(http), Correlation(http), Ip(http), ct)));
api.MapPost("/boletos", async (IssueBoletoBody body, ClaimsPrincipal user, IBank bank, CancellationToken ct) => Results.Ok(await bank.IssueBoletoAsync(UserId(user), body.Amount, body.DueDate, body.Description, ct)));
api.MapPost("/boletos/pay", async (PayBoletoBody body, ClaimsPrincipal user, IBank bank, HttpContext http, CancellationToken ct) => Results.Ok(await bank.PayBoletoAsync(UserId(user), body.Line, Key(http), Correlation(http), Ip(http), ct)));
api.MapPost("/cards/purchases", async (PurchaseBody body, ClaimsPrincipal user, IBank bank, HttpContext http, CancellationToken ct) => Results.Ok(await bank.BuyAsync(UserId(user), body.CardId, body.Merchant, body.Amount, Key(http), Correlation(http), Ip(http), ct)));
api.MapPost("/cards/invoices/{invoiceId:guid}/pay", async (Guid invoiceId, ClaimsPrincipal user, IBank bank, HttpContext http, CancellationToken ct) => Results.Ok(await bank.PayInvoiceAsync(UserId(user), invoiceId, Key(http), Correlation(http), Ip(http), ct)));
api.MapPost("/clock/advance", async (ClaimsPrincipal user, IBank bank, HttpContext http, CancellationToken ct) => Results.Ok(await bank.AdvanceAsync(UserId(user), Correlation(http), Ip(http), ct)));

app.Run();

static Guid UserId(ClaimsPrincipal user) => Guid.Parse(user.FindFirst("sub")!.Value);
static string Key(HttpContext http) => http.Request.Headers["Idempotency-Key"].ToString();
static string Correlation(HttpContext http) => string.IsNullOrWhiteSpace(http.Request.Headers["X-Correlation-Id"]) ? http.TraceIdentifier : http.Request.Headers["X-Correlation-Id"].ToString();
static string? Ip(HttpContext http)
{
    var forwarded = http.Request.Headers["X-Forwarded-For"].ToString();
    if (!string.IsNullOrWhiteSpace(forwarded))
        return forwarded.Split(',')[0].Trim();
    return http.Connection.RemoteIpAddress?.ToString();
}

internal sealed record ProvisionBody(string Name, string Cpf);
internal sealed record PixKeyBody(string Kind, string Value);
internal sealed record PixBody(string Key, decimal Amount);
internal sealed record TedBody(string Agency, string Number, decimal Amount);
internal sealed record SavingsBody(string Direction, decimal Amount);
internal sealed record IssueBoletoBody(decimal Amount, DateOnly DueDate, string Description);
internal sealed record PayBoletoBody(string Line);
internal sealed record PurchaseBody(Guid CardId, string Merchant, decimal Amount);
