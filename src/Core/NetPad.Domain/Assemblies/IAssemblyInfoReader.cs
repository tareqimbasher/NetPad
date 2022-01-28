namespace NetPad.Assemblies
{
    public interface IAssemblyInfoReader
    {
        public string[] GetNamespaces(byte[] assembly);
    }
}
