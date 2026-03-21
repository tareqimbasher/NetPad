namespace NetPad.Data;

/// <summary>
/// Shared properties for anything that connects to a database server:
/// both <see cref="DatabaseConnection"/> (individual database) and
/// <see cref="DatabaseServerConnection"/> (server-level).
/// </summary>
public interface IDatabaseConnection
{
    string? Host { get; }
    string? Port { get; }
    string? UserId { get; }
    string? Password { get; }
    string? ConnectionStringAugment { get; }
    bool ContainsProductionData { get; }
}
