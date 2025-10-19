using System.Security.Cryptography;
using System.Text;

namespace NetPad.Common;

/// <summary>
/// Provides functionality for creating deterministic, name-based UUID version 5 (SHA-1) values
/// as defined in RFC 4122. These UUIDs are derived from a namespace UUID and a name string.
/// </summary>
internal static class Uuid5
{
    /// <summary>
    /// The application-wide namespace UUID used as the default when creating
    /// version 5 UUIDs. This value should remain constant for the lifetime of the application.
    /// </summary>
    public static readonly Guid AppNamespace = new("a16ece7c-e347-48aa-88d0-63a5271bb506");

    /// <summary>
    /// Creates a deterministic UUID version 5 (SHA-1) using the specified name and the default
    /// <see cref="AppNamespace"/> as the namespace.
    /// </summary>
    /// <param name="name">The name from which to generate the UUID. Must not be <c>null</c>.</param>
    /// <returns>
    /// A <see cref="Guid"/> representing the version 5 UUID derived from the given name
    /// within the default application namespace.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is <c>null</c>.</exception>
    public static Guid Create(string name) => Create(AppNamespace, name);

    /// <summary>
    /// Creates a deterministic UUID version 5 (SHA-1) using the specified namespace UUID and name,
    /// following the algorithm defined in RFC 4122.
    /// </summary>
    /// <param name="ns">
    /// The namespace UUID used to scope the generated UUID.
    /// Using a consistent namespace ensures unique identifiers across different domains of names.
    /// </param>
    /// <param name="name">The name from which to generate the UUID. Must not be <c>null</c>.</param>
    /// <returns>
    /// A <see cref="Guid"/> representing the version 5 UUID derived from the specified
    /// namespace and name.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is <c>null</c>.</exception>
    public static Guid Create(Guid ns, string name)
    {
        if (name is null) throw new ArgumentNullException(nameof(name));

        // RFC 4122: namespace bytes must be in network order
        byte[] nsBytes = ns.ToByteArray();
        ToNetworkOrder(nsBytes);

        byte[] nameBytes = Encoding.UTF8.GetBytes(name);

        byte[] data = new byte[nsBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(nsBytes, 0, data, 0, nsBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, data, nsBytes.Length, nameBytes.Length);

        byte[] hash = SHA1.HashData(data); // 20 bytes

        Span<byte> g = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(g);

        // Set version (5) and variant (RFC 4122)
        g[6] = (byte)((g[6] & 0x0F) | 0x50);
        g[8] = (byte)((g[8] & 0x3F) | 0x80);

        return FromNetworkOrderToGuid(g);
    }

    private static Guid FromNetworkOrderToGuid(ReadOnlySpan<byte> net)
    {
        // Swap back to Guid's byte order before constructing Guid
        var b = net.ToArray();
        Array.Reverse(b, 0, 4);        // time_low
        Array.Reverse(b, 4, 2);        // time_mid
        Array.Reverse(b, 6, 2);        // time_hi_and_version
        return new Guid(b);
    }

    private static void ToNetworkOrder(byte[] b)
    {
        // Guid's byte[] uses little-endian for the first three fields; RFC wants big-endian.
        Array.Reverse(b, 0, 4); // time_low
        Array.Reverse(b, 4, 2); // time_mid
        Array.Reverse(b, 6, 2); // time_hi_and_version
        // bytes 8..15 are already in network order
    }
}
