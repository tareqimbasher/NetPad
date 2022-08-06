import {CancellationToken, editor, languages, Position} from "monaco-editor";
import {EditorUtil} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import * as api from "../api";
import {Converter} from "../utils";

export class OmniSharpImplementationProvider implements languages.ImplementationProvider {
    constructor(@IOmniSharpService private readonly omnisharpService: IOmniSharpService) {
    }

    public async provideImplementation(model: editor.ITextModel, position: Position, token: CancellationToken)
        : Promise<languages.Definition | languages.LocationLink[]> {

        const scriptId = EditorUtil.getScriptId(model);

        const response = await this.omnisharpService.findImplementations(scriptId, new api.FindImplementationsRequest({
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
                range: Converter.apiQuickFixToMonacoRange(qf)
            }
        });
    }
}
