namespace Dialysis.BuildingBlocks.Hipaa.Encryption;

/// <summary>
/// Encrypts / decrypts protected health information (PHI) at the column level using a stable
/// purpose so values are portable across replicas that share the same Data Protection key ring.
/// HIPAA §164.312(a)(2)(iv) Encryption and Decryption — the "addressable" specification we treat
/// as required for PHI at rest.
/// </summary>
public interface IPhiProtector
{
    /// <summary>Returns base64-encoded ciphertext suitable for round-tripping through EF column storage.</summary>
    string Encrypt(string plaintext);

    /// <summary>Reverses <see cref="Encrypt"/>. Throws on tamper or wrong key — caller surfaces a friendly error.</summary>
    string Decrypt(string ciphertext);
}
