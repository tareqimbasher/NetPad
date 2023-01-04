using System;
using System.IO;
using NetPad.Exceptions;

namespace NetPad.DotNet;

public class AssemblyFileReference : Reference
{
    public AssemblyFileReference(string assemblyPath)
        : base(!string.IsNullOrWhiteSpace(assemblyPath) ? Path.GetFileName(assemblyPath) : "(Unknown)")
    {
        AssemblyPath = assemblyPath ?? throw new ArgumentNullException(nameof(assemblyPath));
    }

    public string AssemblyPath { get; }

    public override void EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(AssemblyPath))
            throw new InvalidReferenceException(this, $"{nameof(AssemblyPath)} is required.");
    }
}
