using NetPad.DotNet.CodeAnalysis;

namespace NetPad.Compilation.Scripts.Dependencies;

/// <summary>
/// Code that should be added to a script for it to run.
/// </summary>
public record CodeDependency(SourceCodeCollection Code);
