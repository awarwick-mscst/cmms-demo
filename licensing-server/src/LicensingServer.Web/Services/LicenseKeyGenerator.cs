using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LicensingServer.Web.Services;

public class LicenseKeyGenerator
{
    private readonly RSA _privateKey;
    private readonly RSA _publicKey;
    private readonly ILogger<LicenseKeyGenerator> _logger;

    public LicenseKeyGenerator(IConfiguration configuration, ILogger<LicenseKeyGenerator> logger)
    {
        _logger = logger;

        var keyPath = configuration["Licensing:PrivateKeyPath"];
        if (!string.IsNullOrEmpty(keyPath) && File.Exists(keyPath))
        {
            var pem = File.ReadAllText(keyPath);
            _privateKey = RSA.Create();
            _privateKey.ImportFromPem(pem);
            _logger.LogInformation("Loaded RSA private key from {Path}", keyPath);
        }
        else
        {
            // Generate a new key pair for development
            _privateKey = RSA.Create(2048);
            var keysDir = Path.Combine(AppContext.BaseDirectory, "keys");
            Directory.CreateDirectory(keysDir);

            var privatePath = Path.Combine(keysDir, "license-private.pem");
            var publicPath = Path.Combine(keysDir, "license-public.pem");

            File.WriteAllText(privatePath, _privateKey.ExportRSAPrivateKeyPem());
            File.WriteAllText(publicPath, _privateKey.ExportRSAPublicKeyPem());

            _logger.LogWarning("Generated new RSA key pair at {KeysDir}. Copy the public key to the CMMS application.", keysDir);
        }

        // Derive public key from private key
        _publicKey = RSA.Create();
        _publicKey.ImportRSAPublicKey(_privateKey.ExportRSAPublicKey(), out _);
    }

    public string GenerateLicenseKey(LicensePayload payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(json);
        var signature = _privateKey.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var payloadBase64 = Convert.ToBase64String(payloadBytes);
        var signatureBase64 = Convert.ToBase64String(signature);

        return $"{payloadBase64}.{signatureBase64}";
    }

    public LicensePayload? VerifyAndDecode(string licenseKey)
    {
        try
        {
            var parts = licenseKey.Split('.');
            if (parts.Length != 2) return null;

            var payloadBytes = Convert.FromBase64String(parts[0]);
            var signatureBytes = Convert.FromBase64String(parts[1]);

            var valid = _publicKey.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            if (!valid) return null;

            return JsonSerializer.Deserialize<LicensePayload>(payloadBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to verify license key");
            return null;
        }
    }

    public string GetPublicKeyPem()
    {
        return _privateKey.ExportRSAPublicKeyPem();
    }
}

public class LicensePayload
{
    public int LicenseId { get; set; }
    public int CustomerId { get; set; }
    public string Tier { get; set; } = string.Empty;
    public int MaxActivations { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string[] Features { get; set; } = Array.Empty<string>();
}
