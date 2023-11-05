namespace NetPad.Configuration;

public interface ISettingsOptions
{
    /// <summary>
    /// Sets default values to properties that are missing values.
    /// </summary>
    void DefaultMissingValues();
}
