using System.Text.Json.Serialization;

namespace NetPad.Configuration;

/// <summary>
/// This should be moved to the OmniSharp plugin
/// </summary>
public class OmniSharpOptions : ISettingsOptions
{
    public OmniSharpOptions()
    {
        Enabled = true;
        EnableAnalyzersSupport = true;
        EnableImportCompletion = true;
        EnableSemanticHighlighting = true;
        EnableCodeLensReferences = true;
        DefaultMissingValues();
    }

    [JsonInclude] public bool Enabled { get; private set; }
    [JsonInclude] public string? ExecutablePath { get; private set; }
    [JsonInclude] public bool EnableAnalyzersSupport { get; private set; }
    [JsonInclude] public bool EnableImportCompletion { get; private set; }
    [JsonInclude] public bool EnableSemanticHighlighting { get; private set; }
    [JsonInclude] public bool EnableCodeLensReferences { get; private set; }
    [JsonInclude] public OmniSharpDiagnosticsOptions Diagnostics { get; private set; } = null!;
    [JsonInclude] public OmniSharpInlayHintsOptions InlayHints { get; private set; } = null!;

    public OmniSharpOptions SetEnabled(bool enabled)
    {
        Enabled = enabled;
        return this;
    }

    public OmniSharpOptions SetExecutablePath(string? executablePath)
    {
        ExecutablePath = string.IsNullOrWhiteSpace(executablePath) ? null : executablePath;
        return this;
    }

    public OmniSharpOptions SetEnableAnalyzersSupport(bool enableAnalyzersSupport)
    {
        EnableAnalyzersSupport = enableAnalyzersSupport;
        return this;
    }

    public OmniSharpOptions SetEnableImportCompletion(bool enableImportCompletion)
    {
        EnableImportCompletion = enableImportCompletion;
        return this;
    }

    public OmniSharpOptions SetEnableSemanticHighlighting(bool enableSemanticHighlighting)
    {
        EnableSemanticHighlighting = enableSemanticHighlighting;
        return this;
    }

    public OmniSharpOptions SetEnableCodeLensReferences(bool enableCodeLensReferences)
    {
        EnableCodeLensReferences = enableCodeLensReferences;
        return this;
    }

    public OmniSharpOptions SetDiagnosticsOptions(OmniSharpDiagnosticsOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        Diagnostics
            .SetEnabled(options.Enabled)
            .SetEnableInfo(options.EnableInfo)
            .SetEnableWarnings(options.EnableWarnings)
            .SetEnableHints(options.EnableHints);

        return this;
    }

    public OmniSharpOptions SetInlayHintsOptions(OmniSharpInlayHintsOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        InlayHints
            .SetEnableParameters(options.EnableParameters)
            .SetEnableIndexerParameters(options.EnableIndexerParameters)
            .SetEnableLiteralParameters(options.EnableLiteralParameters)
            .SetEnableObjectCreationParameters(options.EnableObjectCreationParameters)
            .SetEnableOtherParameters(options.EnableOtherParameters)
            .SetSuppressForParametersThatDifferOnlyBySuffix(options.SuppressForParametersThatDifferOnlyBySuffix)
            .SetSuppressForParametersThatMatchMethodIntent(options.SuppressForParametersThatMatchMethodIntent)
            .SetSuppressForParametersThatMatchArgumentName(options.SuppressForParametersThatMatchArgumentName)
            .SetEnableTypes(options.EnableTypes)
            .SetEnableImplicitVariableTypes(options.EnableImplicitVariableTypes)
            .SetEnableLambdaParameterTypes(options.EnableLambdaParameterTypes)
            .SetEnableImplicitObjectCreation(options.EnableImplicitObjectCreation);

        return this;
    }

    public void DefaultMissingValues()
    {
        (Diagnostics ??= new OmniSharpDiagnosticsOptions()).DefaultMissingValues();
        (InlayHints ??= new OmniSharpInlayHintsOptions()).DefaultMissingValues();
    }
}

public class OmniSharpDiagnosticsOptions : ISettingsOptions
{
    public OmniSharpDiagnosticsOptions()
    {
        Enabled = true;
        EnableInfo = true;
        EnableWarnings = true;
        EnableHints = true;
        DefaultMissingValues();
    }

    [JsonInclude] public bool Enabled { get; private set; }
    [JsonInclude] public bool EnableInfo { get; private set; }
    [JsonInclude] public bool EnableWarnings { get; private set; }
    [JsonInclude] public bool EnableHints { get; private set; }

    public OmniSharpDiagnosticsOptions SetEnabled(bool enabled)
    {
        Enabled = enabled;
        return this;
    }

    public OmniSharpDiagnosticsOptions SetEnableInfo(bool enableInfo)
    {
        EnableInfo = enableInfo;
        return this;
    }

    public OmniSharpDiagnosticsOptions SetEnableWarnings(bool enableWarnings)
    {
        EnableWarnings = enableWarnings;
        return this;
    }

    public OmniSharpDiagnosticsOptions SetEnableHints(bool enableHints)
    {
        EnableHints = enableHints;
        return this;
    }

    public void DefaultMissingValues()
    {
    }
}

public class OmniSharpInlayHintsOptions : ISettingsOptions
{
    public OmniSharpInlayHintsOptions()
    {
        EnableParameters = true;
        EnableOtherParameters = true;
        SuppressForParametersThatDifferOnlyBySuffix = true;
        SuppressForParametersThatMatchMethodIntent = true;
        SuppressForParametersThatMatchArgumentName = true;
        EnableTypes = true;
        EnableLambdaParameterTypes = true;
        EnableImplicitObjectCreation = true;
        DefaultMissingValues();
    }

    [JsonInclude] public bool EnableParameters { get; private set; }
    [JsonInclude] public bool EnableIndexerParameters { get; private set; }
    [JsonInclude] public bool EnableLiteralParameters { get; private set; }
    [JsonInclude] public bool EnableObjectCreationParameters { get; private set; }
    [JsonInclude] public bool EnableOtherParameters { get; private set; }
    [JsonInclude] public bool SuppressForParametersThatDifferOnlyBySuffix { get; private set; }
    [JsonInclude] public bool SuppressForParametersThatMatchMethodIntent { get; private set; }
    [JsonInclude] public bool SuppressForParametersThatMatchArgumentName { get; private set; }
    [JsonInclude] public bool EnableTypes { get; private set; }
    [JsonInclude] public bool EnableImplicitVariableTypes { get; private set; }
    [JsonInclude] public bool EnableLambdaParameterTypes { get; private set; }
    [JsonInclude] public bool EnableImplicitObjectCreation { get; private set; }


    public OmniSharpInlayHintsOptions SetEnableParameters(bool enableParameters)
    {
        EnableParameters = enableParameters;
        return this;
    }

    public OmniSharpInlayHintsOptions SetEnableIndexerParameters(bool enableIndexerParameters)
    {
        EnableIndexerParameters = enableIndexerParameters;
        return this;
    }

    public OmniSharpInlayHintsOptions SetEnableLiteralParameters(bool enableLiteralParameters)
    {
        EnableLiteralParameters = enableLiteralParameters;
        return this;
    }

    public OmniSharpInlayHintsOptions SetEnableObjectCreationParameters(bool enableObjectCreationParameters)
    {
        EnableObjectCreationParameters = enableObjectCreationParameters;
        return this;
    }

    public OmniSharpInlayHintsOptions SetEnableOtherParameters(bool enableOtherParameters)
    {
        EnableOtherParameters = enableOtherParameters;
        return this;
    }

    public OmniSharpInlayHintsOptions SetSuppressForParametersThatDifferOnlyBySuffix(bool suppressForParametersThatDifferOnlyBySuffix)
    {
        SuppressForParametersThatDifferOnlyBySuffix = suppressForParametersThatDifferOnlyBySuffix;
        return this;
    }

    public OmniSharpInlayHintsOptions SetSuppressForParametersThatMatchMethodIntent(bool suppressForParametersThatMatchMethodIntent)
    {
        SuppressForParametersThatMatchMethodIntent = suppressForParametersThatMatchMethodIntent;
        return this;
    }

    public OmniSharpInlayHintsOptions SetSuppressForParametersThatMatchArgumentName(bool suppressForParametersThatMatchArgumentName)
    {
        SuppressForParametersThatMatchArgumentName = suppressForParametersThatMatchArgumentName;
        return this;
    }

    public OmniSharpInlayHintsOptions SetEnableTypes(bool enableTypes)
    {
        EnableTypes = enableTypes;
        return this;
    }

    public OmniSharpInlayHintsOptions SetEnableImplicitVariableTypes(bool enableImplicitVariableTypes)
    {
        EnableImplicitVariableTypes = enableImplicitVariableTypes;
        return this;
    }

    public OmniSharpInlayHintsOptions SetEnableLambdaParameterTypes(bool enableLambdaParameterTypes)
    {
        EnableLambdaParameterTypes = enableLambdaParameterTypes;
        return this;
    }

    public OmniSharpInlayHintsOptions SetEnableImplicitObjectCreation(bool enableImplicitObjectCreation)
    {
        EnableImplicitObjectCreation = enableImplicitObjectCreation;
        return this;
    }

    public void DefaultMissingValues()
    {
    }
}
