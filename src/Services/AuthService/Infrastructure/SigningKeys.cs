using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Bankcore.Auth.Infrastructure;

public sealed class SigningKeys
{
    public const string Issuer = "bankcore-auth";
    public const string Audience = "bankcore";

    private SigningKeys(ECDsa privateKey, string publicPem)
    {
        PrivateKey = new ECDsaSecurityKey(privateKey) { KeyId = "bankcore-dev" };
        PublicPem = publicPem;
    }

    public ECDsaSecurityKey PrivateKey { get; }
    public string PublicPem { get; }

    public static SigningKeys Load(string directory)
    {
        Directory.CreateDirectory(directory);
        var privatePath = Path.Combine(directory, "auth-private.pem");
        var publicPath = Path.Combine(directory, "auth-public.pem");
        if (!File.Exists(privatePath) || !File.Exists(publicPath))
        {
            using var created = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            File.WriteAllText(privatePath, created.ExportPkcs8PrivateKeyPem());
            File.WriteAllText(publicPath, created.ExportSubjectPublicKeyInfoPem());
        }

        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(File.ReadAllText(privatePath));
        return new SigningKeys(ecdsa, File.ReadAllText(publicPath));
    }
}
