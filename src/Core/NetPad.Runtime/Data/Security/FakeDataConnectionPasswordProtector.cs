namespace NetPad.Data.Security;

/// <summary>
/// A data connection password protector that doesn't actually protect/unprotect the password, and instead
/// returns a hard-coded string, ex. "hidden".
/// </summary>
public class FakeDataConnectionPasswordProtector : IDataConnectionPasswordProtector
{
    public byte[] Protect(byte[] plaintext) => "<hidden>"u8.ToArray();

    public string Protect(string plaintext) => "<hidden>";

    public bool TryUnprotect(string protectedData, out string unprotectedData)
    {
        unprotectedData = Protect(protectedData);
        return true;
    }

    public byte[] Unprotect(byte[] protectedData) => Protect(protectedData);

    public string Unprotect(string protectedData) => "<hidden>";

    public bool TryUnprotect(byte[] protectedData, out byte[] unprotectedData)
    {
        unprotectedData = Unprotect(protectedData);
        return true;
    }
}
