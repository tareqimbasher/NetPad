using MediatR;

namespace NetPad.Plugins.OmniSharp.Features.CodeStructure;

public class GetCodeStructureQuery : OmniSharpScriptQuery<CodeStructureResponse?>
{
    public GetCodeStructureQuery(Guid scriptId) : base(scriptId)
    {
    }

    public class Handler : IRequestHandler<GetCodeStructureQuery, CodeStructureResponse?>
    {
        private readonly AppOmniSharpServer _server;

        public Handler(AppOmniSharpServer server)
        {
            _server = server;
        }

        public async Task<CodeStructureResponse?> Handle(GetCodeStructureQuery request, CancellationToken cancellationToken)
        {
            int userCodeStartsOnLine = _server.Project.UserCodeStartsOnLine;

            var response = await _server.OmniSharpServer.SendAsync<CodeStructureResponse>(new OmniSharpCodeStructureRequest
            {
                FileName = _server.Project.ProgramFilePath
            });

            if (response?.Elements == null)
            {
                return response;
            }

            // Correct line numbers
            RecurseCodeElements(response.Elements, null, (element, parent) =>
            {
                if (!element.Ranges.TryGetValue("name", out OmniSharpRange? range)) return;

                element.Ranges["name"] = new()
                {
                    Start = LineCorrecter.AdjustForResponse(userCodeStartsOnLine, range.Start),
                    End = LineCorrecter.AdjustForResponse(userCodeStartsOnLine, range.End)
                };
            });

            return response;
        }

        private void RecurseCodeElements(
            IEnumerable<CodeStructureResponse.CodeElement> elements,
            CodeStructureResponse.CodeElement? parent,
            Action<CodeStructureResponse.CodeElement, CodeStructureResponse.CodeElement?> action)
        {
            foreach (var element in elements)
            {
                action(element, parent);

                if (element.Children?.Any() == true)
                {
                    RecurseCodeElements(element.Children, element, action);
                }
            }
        }
    }
}
