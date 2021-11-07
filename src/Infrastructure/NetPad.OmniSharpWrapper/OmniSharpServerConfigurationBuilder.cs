using System;
using NetPad.OmniSharpWrapper.Http;
using NetPad.OmniSharpWrapper.Stdio;

namespace NetPad.OmniSharpWrapper
{
    public class OmniSharpServerConfigurationBuilder
    {
        private OmniSharpServerProtocolType _protocolType = OmniSharpServerProtocolType.NotSet;
        private OmniSharpStdioServerConfigurationBuilder? _stdioConfigurationBuilder;
        private OmniSharpHttpServerConfigurationBuilder? _httpConfigurationBuilder;

        public OmniSharpServerConfigurationBuilder UseStdio(Action<OmniSharpStdioServerConfigurationBuilder> configure)
        {
            EnsureProtocolUnset();
            
            _protocolType = OmniSharpServerProtocolType.Stdio;

            _stdioConfigurationBuilder = new OmniSharpStdioServerConfigurationBuilder();
            configure(_stdioConfigurationBuilder);

            return this;
        }

        public OmniSharpServerConfigurationBuilder UseHttp(Action<OmniSharpHttpServerConfigurationBuilder> configure)
        {
            EnsureProtocolUnset();

            _protocolType = OmniSharpServerProtocolType.Http;

            _httpConfigurationBuilder = new OmniSharpHttpServerConfigurationBuilder();
            configure(_httpConfigurationBuilder);

            return this;
        }

        public OmniSharpServerConfiguration Build()
        {
            return _protocolType switch
            {
                OmniSharpServerProtocolType.Stdio => _stdioConfigurationBuilder!.Build(),
                OmniSharpServerProtocolType.Http => _httpConfigurationBuilder!.Build(),
                OmniSharpServerProtocolType.NotSet => throw new Exception(
                    $"Protocol is not set. Use {nameof(UseStdio)}() or {nameof(UseHttp)}() to specify the protocol."),
                _ => throw new ArgumentOutOfRangeException($"Unknown protocol type: {_protocolType}")
            };
        }
        
        private void EnsureProtocolUnset()
        {
            if (_protocolType != OmniSharpServerProtocolType.NotSet)
                throw new InvalidOperationException($"Protocol is already set to {_protocolType}.");
        }
    }
}