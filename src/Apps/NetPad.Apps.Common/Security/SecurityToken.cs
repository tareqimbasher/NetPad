using System.Security.Cryptography;
using System.Text;

namespace NetPad.Apps.Security;

/// <summary>
/// Generates and validates a per-process security token used to authenticate
/// requests to the local HTTP backend.
/// </summary>
public class SecurityToken
{
    private const string DevToken = "dev";
    private readonly byte[] _tokenBytes;

    public SecurityToken()
    {
        Token = GenerateToken();
        _tokenBytes = Encoding.UTF8.GetBytes(Token);
    }

    public string Token { get; }

    public bool Validate(string? candidate)
    {
        if (candidate == null)
        {
            return false;
        }

#if DEBUG
        if (candidate == DevToken)
        {
            return true;
        }
#endif

        var candidateBytes = Encoding.UTF8.GetBytes(candidate);

        if (candidateBytes.Length != _tokenBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(candidateBytes, _tokenBytes);
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
