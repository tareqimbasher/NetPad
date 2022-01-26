using System.IO;
using NetPad.Exceptions;

namespace NetPad.Scripts
{
    public class AssemblyReference : Reference
    {
        public AssemblyReference(string assemblyPath)
            : base(!string.IsNullOrWhiteSpace(assemblyPath) ? Path.GetFileName(assemblyPath) : "(Unknown)")
        {
            AssemblyPath = assemblyPath;
        }

        public string? AssemblyPath { get; }

        public override void EnsureValid()
        {
            if (string.IsNullOrWhiteSpace(AssemblyPath))
                throw new InvalidReferenceException(this, $"{nameof(AssemblyPath)} is required.");
        }
    }
}
