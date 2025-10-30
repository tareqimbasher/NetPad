using Microsoft.AspNetCore.DataProtection;
using NetPad.Application;

namespace NetPad.Configuration;

internal static class UserDataManager
{
    private static readonly IDataProtector _dataProtector = DataProtectionProvider
        .Create(AppIdentifier.AppId)
        .CreateProtector("UserData");

    public static UserSecrets Secrets { get; } =
        new(AppDataProvider.UserDataDirectoryPath.CombineFilePath("user-secrets.json"), _dataProtector);
}
