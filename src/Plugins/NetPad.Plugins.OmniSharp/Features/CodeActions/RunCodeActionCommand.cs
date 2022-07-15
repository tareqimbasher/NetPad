using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using NetPad.Plugins.OmniSharp.Features.Common.FileOperation;
using Newtonsoft.Json.Linq;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Plugins.OmniSharp.Features.CodeActions;

public class RunCodeActionCommand : OmniSharpScriptCommand<OmniSharpRunCodeActionRequest, RunCodeActionResponse?>
{
    public RunCodeActionCommand(Guid scriptId, OmniSharpRunCodeActionRequest input) : base(scriptId, input)
    {
    }

    public class Handler : IRequestHandler<RunCodeActionCommand, RunCodeActionResponse?>
    {
        private readonly AppOmniSharpServer _server;

        private static readonly JsonSerializerOptions fileOperationResponseCollectionSerializationOptions = new JsonSerializerOptions
        {
            Converters = { new FileOperationResponseCollectionJsonConverter() }
        };

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<RunCodeActionResponse?> Handle(RunCodeActionCommand request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;
            int userCodeStartsOnLine = _server.Project.UserCodeStartsOnLine;

            omniSharpRequest.FileName = _server.Project.ProgramFilePath;
            omniSharpRequest.Line = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Line);

            if (omniSharpRequest.Selection != null)
            {
                omniSharpRequest.Selection = new()
                {
                    Start = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Selection.Start),
                    End = LineCorrecter.AdjustForOmniSharp(userCodeStartsOnLine, omniSharpRequest.Selection.End)
                };
            }

            var responseJToken = await _server.OmniSharpServer.SendAsync<JToken>(omniSharpRequest);

            RunCodeActionResponse? response = DeserializeOmniSharpResponse(responseJToken);

            if (response?.Changes == null)
            {
                return response;
            }

            foreach (var change in response.Changes)
            {
                if (change is not ModifiedFileResponse modifiedFileResponse) continue;
                foreach (var modifiedFileChange in modifiedFileResponse.Changes)
                {
                    modifiedFileChange.StartLine = LineCorrecter.AdjustForResponse(userCodeStartsOnLine, modifiedFileChange.StartLine);
                    modifiedFileChange.EndLine = LineCorrecter.AdjustForResponse(userCodeStartsOnLine, modifiedFileChange.EndLine);
                }
            }

            return response;
        }

        private static RunCodeActionResponse? DeserializeOmniSharpResponse(JToken? responseJToken)
        {
            if (responseJToken?.HasValues != true || responseJToken.Type == JTokenType.Null)
            {
                return null;
            }

            var changesArr = responseJToken["Changes"];

            if (changesArr == null || changesArr.Type == JTokenType.Null)
            {
                return null;
            }


            return new RunCodeActionResponse()
            {
                Changes = JsonSerializer.Deserialize<IEnumerable<FileOperationResponse?>>(changesArr.ToString(),
                    fileOperationResponseCollectionSerializationOptions)
            };
        }
    }

    private class FileOperationResponseCollectionJsonConverter : JsonConverter<IEnumerable<FileOperationResponse?>>
    {
        public override IEnumerable<FileOperationResponse?>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            var fileOperationResponses = new List<FileOperationResponse?>();

            using var jsonDocument = JsonDocument.ParseValue(ref reader);
            var jsonObject = jsonDocument.RootElement;

            foreach (JsonElement element in jsonObject.EnumerateArray())
            {
                var modificationTypeToken = element.GetProperty("ModificationType");

                var modificationType = modificationTypeToken.Deserialize<OmniSharpFileModificationType>();

                FileOperationResponse? fileOperationResponse = modificationType switch
                {
                    OmniSharpFileModificationType.Modified => element.Deserialize<ModifiedFileResponse>(),
                    OmniSharpFileModificationType.Renamed => element.Deserialize<RenamedFileResponse>(),
                    OmniSharpFileModificationType.Opened => element.Deserialize<OpenFileResponse>(),
                    _ => null
                };

                fileOperationResponses.Add(fileOperationResponse);
            }

            return fileOperationResponses;
        }

        public override void Write(Utf8JsonWriter writer, IEnumerable<FileOperationResponse?> value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
