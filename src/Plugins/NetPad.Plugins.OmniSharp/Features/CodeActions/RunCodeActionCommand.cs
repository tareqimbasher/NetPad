using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MediatR;
using NetPad.Plugins.OmniSharp.Features.Common.FileOperation;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Plugins.OmniSharp.Features.CodeActions;

public class RunCodeActionCommand(Guid scriptId, OmniSharpRunCodeActionRequest input)
    : OmniSharpScriptCommand<OmniSharpRunCodeActionRequest, RunCodeActionResponse?>(scriptId, input)
{
    public class Handler(AppOmniSharpServer server) : IRequestHandler<RunCodeActionCommand, RunCodeActionResponse?>
    {
        private static readonly FileOperationResponseCollectionJsonConverter _fileOperationResponseCollectionJsonConverter = new();

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            Converters = { _fileOperationResponseCollectionJsonConverter }
        };

        public async Task<RunCodeActionResponse?> Handle(RunCodeActionCommand request, CancellationToken cancellationToken)
        {
            var omniSharpRequest = request.Input;

            omniSharpRequest.FileName = server.Project.UserProgramFilePath;

            var responseJson = await server.OmniSharpServer.SendAsync<JsonNode>(omniSharpRequest, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            RunCodeActionResponse? response = DeserializeOmniSharpResponse(responseJson);

            return response;
        }

        private static RunCodeActionResponse? DeserializeOmniSharpResponse(JsonNode? responseJson)
        {
            var changesArr = responseJson?["Changes"];

            if (changesArr is not JsonArray)
            {
                return null;
            }

            return new RunCodeActionResponse
            {
                Changes = JsonSerializer.Deserialize<IEnumerable<FileOperationResponse?>>(
                    changesArr.ToJsonString(),
                    _jsonSerializerOptions)
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
