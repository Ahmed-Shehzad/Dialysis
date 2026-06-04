using Microsoft.AspNetCore.DataProtection;

namespace Dialysis.BuildingBlocks.Hipaa.Encryption;

/// <summary>
/// Default <see cref="IPhiProtector"/> backed by ASP.NET Core Data Protection. The purpose string
/// is versioned so a future key-rotation strategy can introduce a v2 purpose alongside the v1
/// reader without breaking already-encrypted columns.
///
/// When the host wires Valkey via <c>AddValkeyDistributedCache(... UseDataProtectionKeyRing=true)</c>
/// the key ring is persisted to Valkey so all replicas share one set of master keys; otherwise
/// keys are kept in memory and ciphertext does not survive a host restart (acceptable for dev /
/// tests, never production — see <c>DataProtectionKeyRingPersistentSafeguardCheck</c>).
/// </summary>
public sealed class DataProtectionPhiProtector : IPhiProtector
{
    /// <summary>Versioned purpose so we can introduce a v2 reader alongside v1 if we ever change algorithms.</summary>
    public const string PurposeV1 = "hipaa.phi.column-encryption.v1";

    private readonly IDataProtector _protector;
    /// <summary>
    /// Default <see cref="IPhiProtector"/> backed by ASP.NET Core Data Protection. The purpose string
    /// is versioned so a future key-rotation strategy can introduce a v2 purpose alongside the v1
    /// reader without breaking already-encrypted columns.
    ///
    /// When the host wires Valkey via <c>AddValkeyDistributedCache(... UseDataProtectionKeyRing=true)</c>
    /// the key ring is persisted to Valkey so all replicas share one set of master keys; otherwise
    /// keys are kept in memory and ciphertext does not survive a host restart (acceptable for dev /
    /// tests, never production — see <c>DataProtectionKeyRingPersistentSafeguardCheck</c>).
    /// </summary>
    public DataProtectionPhiProtector(IDataProtectionProvider provider) => _protector = provider.CreateProtector(PurposeV1);

    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        return _protector.Protect(plaintext);
    }

    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        return _protector.Unprotect(ciphertext);
    }
}
