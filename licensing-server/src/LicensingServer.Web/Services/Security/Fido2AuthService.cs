using Fido2NetLib;
using Fido2NetLib.Objects;
using LicensingServer.Web.Data;
using LicensingServer.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Web.Services.Security;

public class Fido2AuthService
{
    private readonly IFido2 _fido2;
    private readonly LicensingDbContext _db;
    private readonly ILogger<Fido2AuthService> _logger;

    public Fido2AuthService(IFido2 fido2, LicensingDbContext db, ILogger<Fido2AuthService> logger)
    {
        _fido2 = fido2;
        _db = db;
        _logger = logger;
    }

    public CredentialCreateOptions GetRegistrationOptions(AdminUser user, IEnumerable<Models.Fido2Credential>? existingCredentials = null)
    {
        var fidoUser = new Fido2User
        {
            Id = BitConverter.GetBytes(user.Id),
            Name = user.Username,
            DisplayName = user.Username,
        };

        var excludeCredentials = (existingCredentials ?? Enumerable.Empty<Models.Fido2Credential>())
            .Where(c => c.RevokedAt == null)
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToList();

        var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fidoUser,
            ExcludeCredentials = excludeCredentials,
            AuthenticatorSelection = new AuthenticatorSelection
            {
                UserVerification = UserVerificationRequirement.Required,
                ResidentKey = ResidentKeyRequirement.Discouraged,
            },
            AttestationPreference = AttestationConveyancePreference.None,
        });

        return options;
    }

    public async Task<Models.Fido2Credential?> CompleteRegistrationAsync(
        AdminUser user,
        AuthenticatorAttestationRawResponse attestationResponse,
        CredentialCreateOptions originalOptions,
        string deviceName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var registeredCredential = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = attestationResponse,
                OriginalOptions = originalOptions,
                IsCredentialIdUniqueToUserCallback = async (args, ct) =>
                {
                    var exists = await _db.Fido2Credentials
                        .AnyAsync(c => c.CredentialId == args.CredentialId, ct);
                    return !exists;
                },
            }, cancellationToken);

            var credential = new Models.Fido2Credential
            {
                AdminUserId = user.Id,
                CredentialId = registeredCredential.Id,
                PublicKey = registeredCredential.PublicKey,
                SignatureCounter = (long)registeredCredential.SignCount,
                AaGuid = registeredCredential.AaGuid,
                DeviceName = deviceName,
                CredentialType = registeredCredential.Type.ToString(),
                Transports = registeredCredential.Transports != null
                    ? System.Text.Json.JsonSerializer.Serialize(registeredCredential.Transports)
                    : null,
                IsBackupEligible = registeredCredential.IsBackupEligible,
                IsBackupDevice = registeredCredential.IsBackedUp,
                RegisteredAt = DateTime.UtcNow,
            };

            _db.Fido2Credentials.Add(credential);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("FIDO2 credential registered for user {Username}, device {DeviceName}", user.Username, deviceName);

            return credential;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FIDO2 registration failed for user {Username}", user.Username);
            return null;
        }
    }

    public async Task<AssertionOptions> GetAuthenticationOptionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var credentials = await _db.Fido2Credentials
            .Where(c => c.AdminUserId == userId && c.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var allowedCredentials = credentials
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToList();

        var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = allowedCredentials,
            UserVerification = UserVerificationRequirement.Required,
        });

        return options;
    }

    public async Task<bool> CompleteAuthenticationAsync(
        int userId,
        AuthenticatorAssertionRawResponse assertionResponse,
        AssertionOptions originalOptions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Find credential by matching raw ID - load all for the user and match in memory
            var assertionIdBytes = Convert.FromBase64String(
                assertionResponse.Id.Replace('-', '+').Replace('_', '/').PadRight(
                    assertionResponse.Id.Length + (4 - assertionResponse.Id.Length % 4) % 4, '='));

            var userCredentials = await _db.Fido2Credentials
                .Where(c => c.AdminUserId == userId && c.RevokedAt == null)
                .ToListAsync(cancellationToken);

            var credential = userCredentials
                .FirstOrDefault(c => c.CredentialId.AsSpan().SequenceEqual(assertionIdBytes));

            if (credential == null)
            {
                _logger.LogWarning("FIDO2 credential not found for authentication, userId={UserId}", userId);
                return false;
            }

            var result = await _fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = assertionResponse,
                OriginalOptions = originalOptions,
                StoredPublicKey = credential.PublicKey,
                StoredSignatureCounter = (uint)credential.SignatureCounter,
                IsUserHandleOwnerOfCredentialIdCallback = async (args, ct) =>
                {
                    return credential.AdminUserId == userId;
                },
            }, cancellationToken);

            // Update counter
            credential.SignatureCounter = (long)result.SignCount;
            credential.LastUsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("FIDO2 authentication successful for userId={UserId}, device={DeviceName}", userId, credential.DeviceName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FIDO2 authentication failed for userId={UserId}", userId);
            return false;
        }
    }

    public async Task<bool> HasCredentialsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _db.Fido2Credentials
            .AnyAsync(c => c.AdminUserId == userId && c.RevokedAt == null, cancellationToken);
    }
}
