using System;
using Newtonsoft.Json.Linq;

namespace OmniSharp.Stdio.IO
{
    public class ResponseJObject : JObject
    {
        public ResponseJObject(JObject response) : base(response)
        {
        }

        public int RequestSequence()
        {
            return (int)(this["Request_seq"] ?? throw new Exception("Response did not have a value for 'Request_seq'"));
        }
        
        public bool Success()
        {
            return (bool)(this["Success"] ?? throw new Exception("Response did not have a value for 'Success'"));
        }
        
        public TBody? Body<TBody>()
        {
            return (this["Body"] ?? throw new Exception("Response did not have a value for 'Body'")).ToObject<TBody>();
        }
    }
}