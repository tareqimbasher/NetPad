namespace NetPad.OmniSharpWrapper
{
    public class OmniSharpServerConfiguration
    {
        public OmniSharpServerConfiguration(OmniSharpServerProtocolType protocolType)
        {
            ProtocolType = protocolType;
        }

        public OmniSharpServerProtocolType ProtocolType { get; }
    }
}