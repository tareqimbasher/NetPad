namespace NetPad.Data;

public interface IDatabaseConnection
{
    string? Host { get; set; }
    string? Port { get; set; }
    string? UserId { get; set; }
    string? Password { get; set; }
    string? ConnectionStringAugment { get; set; }
    bool ContainsProductionData { get; set; }
}
