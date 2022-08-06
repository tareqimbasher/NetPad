using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OmniSharp.Stdio.IO
{
    public class ResponseJsonObject
    {
        private readonly JsonNode _response;

        public ResponseJsonObject(JsonNode response)
        {
            _response = response;
        }

        public int RequestSequence()
        {
            return (int)(_response["Request_seq"] ?? throw new Exception("Response did not have a value for 'Request_seq'"));
        }

        public bool Success()
        {
            return (bool)(_response["Success"] ?? throw new Exception("Response did not have a value for 'Success'"));
        }

        public TBody? Body<TBody>(JsonSerializerOptions serializerOptions)
        {
            return (_response["Body"] ?? throw new Exception("Response did not have a value for 'Body'")).Deserialize<TBody>(serializerOptions);
        }
    }
}
