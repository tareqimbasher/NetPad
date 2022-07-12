import {CancellationToken, editor, languages, Position, Range} from "monaco-editor";
import {EditorUtil} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import {FindImplementationsRequest} from "../api";

export class OmnisharpImplementationProvider implements languages.ImplementationProvider {
    constructor(@IOmniSharpService private readonly omnisharpService: IOmniSharpService) {
    }

    public async provideImplementation(model: editor.ITextModel, position: Position, token: CancellationToken)
        : Promise<languages.Definition | languages.LocationLink[]> {

        const scriptId = EditorUtil.getScriptId(model);

        const response = await this.omnisharpService.findImplementations(scriptId, new FindImplementationsRequest({
            line: position.lineNumber,
            column: position.column,
            applyChangesTogether: false
        }));

        if (!response || !response.quickFixes) {
            return null;
        }

        return response.quickFixes.map(qf => {
            return {
                uri: model.uri,
                range: new Range(qf.line, qf.column + 1, qf.endLine, qf.endColumn)
            }
        });
    }
}
