namespace O2Html;

/// <summary>
/// Methods in dealing with reference loops.
/// </summary>
public enum ReferenceLoopHandling
{
    /// <summary>
    /// Throw a <see cref="HtmlSerializationException" /> when a loop is encountered.
    /// </summary>
    Error,

    /// <summary>
    /// Ignore loop references and do not serialize.
    /// </summary>
    Ignore,

    /// <summary>
    /// Ignore loop references and serialize to a <see cref="Dom.Elements.CyclicReference"/>.
    /// </summary>
    IgnoreAndSerializeCyclicReference,

    /// <summary>
    /// Serialize loop references.
    /// </summary>
    Serialize
}
