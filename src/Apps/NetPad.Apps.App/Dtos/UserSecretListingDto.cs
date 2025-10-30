namespace NetPad.Dtos;

public class UserSecretListingDto
{
    public required string Key { get; set; }
    public required string ShortValue { get; set; }
    public required DateTime UpdatedAtUtc { get; set; }
}
