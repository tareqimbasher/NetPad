namespace NetPad.OmniSharpWrapper
{
    public class OmniSharpPacket
    {
        public OmniSharpPacket(string type)
        {
            Type = type;
        }
        
        public string Type { get; set; }
        public int Seq { get; set; }
    }

    public class OmniSharpEventPacket : OmniSharpPacket
    {
        public OmniSharpEventPacket() : base("event") { }
        
        public string Event { get; set; }
        public OmniSharpEventPacketBody? Body { get; set; }
    }

    public class OmniSharpEventPacketBody
    {
        public string LogLevel { get; set; }
        public string Name { get; set; }
        public string Message { get; set; }
        
        public string Text { get; set; }
        public string FileName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class OmniSharpRequestPackage : OmniSharpPacket
    {
        public OmniSharpRequestPackage() : base("request") { }
        
        public string Command { get; set; }
        public object Arguments { get; set; }
    }

    public class OmniSharpResponsePacket : OmniSharpPacket
    {
        public OmniSharpResponsePacket() : base("response") { }

        public int Request_seq { get; set; }

        public string Command { get; set; }

        public bool Running { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }

        public object Body { get; set; }
    }
}