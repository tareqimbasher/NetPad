using NetPad.Data;

namespace NetPad.Apps.Common.Tests.Data;

public class NullDataConnectionPasswordProtector : IDataConnectionPasswordProtector
{
    public byte[] Protect(byte[] plaintext) => plaintext;

    public string Protect(string plaintext) => plaintext;

    public bool TryUnprotect(byte[] protectedData, out byte[] unprotectedData)
    {
        unprotectedData = protectedData;
        return true;
    }

    public byte[] Unprotect(byte[] protectedData) => protectedData;

    public string Unprotect(string protectedData) => protectedData;

    public bool TryUnprotect(string protectedData, out string unprotectedData)
    {
        unprotectedData = protectedData;
        return true;
    }
}
