namespace NetPad.OmniSharpWrapper.Stdio.Models
{
    public class RequestPacket
    {
        public RequestPacket(int sequence, string command, object arguments)
        {
            Seq = sequence;
            Command = command;
            Arguments = arguments;
        }
        
        public string Command { get; set; }
        public int Seq { get; set; }
        public object Arguments { get; set; }
    }
}