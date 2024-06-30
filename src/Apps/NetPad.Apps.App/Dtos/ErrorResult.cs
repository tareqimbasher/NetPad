namespace NetPad.Dtos;

public class ErrorResult(string message, string? details = null)
{
    public string Message { get; } = message;
    public string? Details { get; } = details;
}
