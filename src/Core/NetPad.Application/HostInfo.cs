namespace NetPad
{
    public class HostInfo
    {
        public HostInfo()
        {
            HostUrl = "http://localhost";
        }

        public string HostUrl { get; private set; }

        public void SetHostUrl(string url)
        {
            HostUrl = url;
        }
    }
}
