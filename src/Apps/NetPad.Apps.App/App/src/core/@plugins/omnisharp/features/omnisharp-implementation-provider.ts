import {CancellationToken, editor, languages, Position} from "monaco-editor";
import {EditorUtil, IImplementationProvider} from "@application";
import * as api from "../api";
import {Converter} from "../utils";
import {FeatureProvider} from "./feature-provider";

export class OmniSharpImplementationProvider extends FeatureProvider implements IImplementationProvider {
    public async provideImplementation(model: editor.ITextModel, position: Position, token: CancellationToken)
        : Promise<languages.Definition | languages.LocationLink[]> {

        const scriptId = EditorUtil.getScriptId(model);

        const response = await this.omnisharpService.findImplementations(scriptId, new api.FindImplementationsRequest({
            line: position.lineNumber,
            column: position.column,
            applyChangesTogether: false
        }), this.getAbortSignal(token));

        if (!response || !response.quickFixes) {
            return [];
        }

        return response.quickFixes.map(qf => {
            return {
                uri: model.uri,
                range: Converter.apiQuickFixToMonacoRange(qf)
            }
        });
    }
}
