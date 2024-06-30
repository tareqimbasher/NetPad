using System.IO;
using NetPad.Exceptions;

namespace NetPad.DotNet;

public class AssemblyFileReference(string assemblyPath)
    : Reference(!string.IsNullOrWhiteSpace(assemblyPath) ? Path.GetFileName(assemblyPath) : "(Unknown)")
{
    public string AssemblyPath { get; } = assemblyPath ?? throw new ArgumentNullException(nameof(assemblyPath));

    public override void EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(AssemblyPath))
            throw new InvalidReferenceException(this, $"{nameof(AssemblyPath)} is required.");
    }
}
