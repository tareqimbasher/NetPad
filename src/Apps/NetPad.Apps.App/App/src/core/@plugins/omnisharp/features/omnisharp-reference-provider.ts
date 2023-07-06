import {CancellationToken, editor, languages, Position} from "monaco-editor";
import {EditorUtil, IReferenceProvider} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import * as api from "../api";
import {Converter} from "../utils";

export class OmniSharpReferenceProvider implements IReferenceProvider {
    constructor(@IOmniSharpService private readonly omnisharpService: IOmniSharpService) {
    }

    public async provideReferences(model: editor.ITextModel, position: Position, context: languages.ReferenceContext, token: CancellationToken)
        : Promise<languages.Location[]> {

        return await OmniSharpReferenceProvider.findUsages(model, this.omnisharpService, position.lineNumber, position.column, true, token) || [];
    }

    public static async findUsages(
        model: editor.ITextModel,
        omnisharpService: IOmniSharpService,
        lineNumber: number,
        column: number,
        excludeDefinition: boolean,
        token: CancellationToken): Promise<languages.Location[] | null> {

        const scriptId = EditorUtil.getScriptId(model);

        const response = await omnisharpService.findUsages(scriptId, new api.FindUsagesRequest({
            onlyThisFile: true,
            excludeDefinition: excludeDefinition,
            line: lineNumber,
            column: column,
            applyChangesTogether: false
        }), new AbortController().signalFrom(token));

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
