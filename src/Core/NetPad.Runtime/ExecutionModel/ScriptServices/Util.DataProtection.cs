using Microsoft.AspNetCore.DataProtection;
using NetPad.Configuration;

namespace NetPad.ExecutionModel.ScriptServices;

public static partial class Util
{
    /// <summary>
    /// Cryptographically protects a piece of plaintext data.
    /// </summary>
    /// <param name="plaintext">The plaintext data to protect.</param>
    /// <returns>The protected form of the plaintext data.</returns>
    public static byte[] Protect(byte[] plaintext) => _dataProtector.Protect(plaintext);

    /// <summary>
    /// Cryptographically protects a piece of plaintext data.
    /// </summary>
    /// <param name="plaintext">The plaintext data to protect.</param>
    /// <returns>The protected form of the plaintext data.</returns>
    public static string Protect(string plaintext) => _dataProtector.Protect(plaintext);

    /// <summary>
    /// Cryptographically unprotects a piece of protected data.
    /// </summary>
    /// <param name="protectedData">The protected data to unprotect.</param>
    /// <returns>The plaintext form of the protected data.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown if the protected data is invalid or malformed.
    /// </exception>
    public static byte[] Unprotect(byte[] protectedData) => _dataProtector.Unprotect(protectedData);

    /// <summary>
    /// Cryptographically unprotects a piece of protected data.
    /// </summary>
    /// <param name="protectedData">The protected data to unprotect.</param>
    /// <returns>The plaintext form of the protected data.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// Thrown if the protected data is invalid or malformed.
    /// </exception>
    public static string Unprotect(string protectedData) => _dataProtector.Unprotect(protectedData);

    /// <summary>
    /// Provides methods for storing and retrieving user secrets
    /// </summary>
    public static UserSecrets Secrets => UserDataManager.Secrets;
}
