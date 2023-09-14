namespace NetPad.Dtos;

public class ErrorResult
{
    public ErrorResult(string message, string? details = null)
    {
        Message = message;
        Details = details;
    }

    public string Message { get; }
    public string? Details { get; }
}
