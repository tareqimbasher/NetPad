namespace OmniSharp
{
    public abstract class OmniSharpServerConfiguration
    {
        protected OmniSharpServerConfiguration(OmniSharpServerProtocolType protocolType)
        {
            ProtocolType = protocolType;
        }

        public OmniSharpServerProtocolType ProtocolType { get; }
    }
}