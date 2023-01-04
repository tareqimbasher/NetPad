using System.Diagnostics.CodeAnalysis;

namespace NetPad.Data;

public interface IDataConnectionPasswordProtector
{
    byte[] Protect(byte[] plaintext);
    string Protect(string plaintext);
    bool TryUnprotect(byte[] protectedData, [MaybeNullWhen(false)] out byte[] unprotectedData);

    byte[] Unprotect(byte[] protectedData);
    string Unprotect(string protectedData);
    bool TryUnprotect(string protectedData, [MaybeNullWhen(false)] out string unprotectedData);
}
