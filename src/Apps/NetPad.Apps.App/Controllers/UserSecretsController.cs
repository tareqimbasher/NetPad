using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NetPad.Configuration;
using NetPad.Dtos;

namespace NetPad.Controllers;

[ApiController]
[Route("user-secrets")]
public class UserSecretsController : ControllerBase
{
    [HttpGet]
    public UserSecretListingDto[] GetAll()
    {
        return UserDataManager.Secrets.List()
            .Select(ToDto)
            .ToArray();
    }

    [HttpGet("unprotected-value")]
    public ActionResult<string> GetUnprotectedValue([FromQuery] string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest("Key cannot be empty");
        }

        return UserDataManager.Secrets.Get(key);
    }

    [HttpPut]
    public UserSecretListingDto Save([FromQuery] string key, [FromBody] string value)
    {
        UserDataManager.Secrets.Save(key, value);
        var secret = UserDataManager.Secrets.GetSecret(key)
                     ?? throw new Exception($"User secret with key '{key}' could not be found after saving");

        return ToDto(secret);
    }

    [HttpDelete]
    public IActionResult Delete([FromQuery] string key)
    {
        UserDataManager.Secrets.Delete(key);
        return Ok();
    }

    private static UserSecretListingDto ToDto(UserSecret secret)
    {
        return new UserSecretListingDto
        {
            Key = secret.Key,
            ShortValue = secret.Value?.Truncate(100, true) ?? string.Empty,
            UpdatedAtUtc = secret.UpdatedAtUtc,
        };
    }
}
