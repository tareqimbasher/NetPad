namespace O2Html;

/// <summary>
/// What kind of value a .NET type holds, as far as presentation is concerned.
/// </summary>
public enum ValueKind
{
    /// <summary>
    /// Plain text.
    /// </summary>
    Text = 0,

    /// <summary>
    /// A number.
    /// </summary>
    Numeric = 1,

    /// <summary>
    /// A <see cref="bool"/>.
    /// </summary>
    Boolean = 2,

    /// <summary>
    /// An enum member.
    /// </summary>
    Enum = 3,

    /// <summary>
    /// A date, time, or duration.
    /// </summary>
    Temporal = 4
}
