using System.Security.Cryptography;
using System.Text;

namespace Dialysis.Simulation.Contracts;

/// <summary>
/// Produces a stable GUID from a string key so simulation output is reproducible: the same seed +
/// scenario + tenant always yields the same ids. Implemented as an RFC-4122 §4.3 name-based UUID
/// (SHA-1 over a fixed namespace + the name), so it never depends on wall-clock time or
/// <see cref="Guid.NewGuid"/>.
/// </summary>
public static class DeterministicGuid
{
    // Fixed namespace GUID for the simulation platform (arbitrary but constant).
    private static readonly byte[] _namespaceBytes =
        new Guid("8f6b9c20-7d3e-4a1b-9b2c-1e5a7f0c4d33").ToByteArray();

    /// <summary>Derives a deterministic GUID from <paramref name="name"/>.</summary>
    public static Guid From(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var nameBytes = Encoding.UTF8.GetBytes(name);
        var combined = new byte[_namespaceBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(_namespaceBytes, 0, combined, 0, _namespaceBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, combined, _namespaceBytes.Length, nameBytes.Length);

        var hash = SHA1.HashData(combined);
        var guidBytes = new byte[16];
        Buffer.BlockCopy(hash, 0, guidBytes, 0, 16);

        // Set version (5) and the RFC-4122 variant bits.
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes);
    }

    /// <summary>Derives a deterministic GUID from a session id and a logical record key.</summary>
    public static Guid From(Guid sessionId, string recordKey) =>
        From($"{sessionId:N}:{recordKey}");
}
