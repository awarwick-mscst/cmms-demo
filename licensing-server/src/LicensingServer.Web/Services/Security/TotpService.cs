using System.Security.Cryptography;
using OtpNet;
using QRCoder;

namespace LicensingServer.Web.Services.Security;

public class TotpService
{
    private const int SecretSize = 20; // 160 bits
    private const int TotpSize = 6;
    private const int TimeStepSeconds = 30;
    private const int VerificationWindow = 1; // ±1 step (90 seconds total)
    private const string Issuer = "CMMS Licensing";

    public string GenerateSecret()
    {
        var secret = RandomNumberGenerator.GetBytes(SecretSize);
        return Base32Encoding.ToString(secret);
    }

    public string GenerateQrCodeUri(string username, string base32Secret)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(username)}" +
               $"?secret={base32Secret}&issuer={Uri.EscapeDataString(Issuer)}&algorithm=SHA1&digits={TotpSize}&period={TimeStepSeconds}";
    }

    public byte[] GenerateQrCodePng(string uri)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(5);
    }

    public bool VerifyCode(string base32Secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != TotpSize)
            return false;

        var secretBytes = Base32Encoding.ToBytes(base32Secret);
        var totp = new Totp(secretBytes, step: TimeStepSeconds, totpSize: TotpSize);

        return totp.VerifyTotp(code, out _, new VerificationWindow(VerificationWindow, VerificationWindow));
    }

    public List<string> GenerateRecoveryCodes(int count = 8)
    {
        var codes = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            var bytes = RandomNumberGenerator.GetBytes(5);
            var code = Convert.ToHexString(bytes).ToLowerInvariant();
            // Format as XXXXX-XXXXX
            codes.Add($"{code[..5]}-{code[5..]}");
        }
        return codes;
    }

    public bool VerifyRecoveryCode(string code, List<string> validCodes)
    {
        var normalized = code.Trim().ToLowerInvariant();
        return validCodes.Contains(normalized);
    }
}
