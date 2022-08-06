import {CancellationToken, editor, languages, Position, Range} from "monaco-editor";
import {EditorUtil} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import * as api from "../api";
import {Converter} from "../utils";

export class OmniSharpReferenceProvider implements languages.ReferenceProvider {
    constructor(@IOmniSharpService private readonly omnisharpService: IOmniSharpService) {
    }

    public async provideReferences(model: editor.ITextModel, position: Position, context: languages.ReferenceContext, token: CancellationToken)
        : Promise<languages.Location[]> {

        return await OmniSharpReferenceProvider.findUsages(model, this.omnisharpService, position.lineNumber, position.column);
    }

    public static async findUsages(
        model: editor.ITextModel,
        omnisharpService: IOmniSharpService,
        lineNumber: number,
        column: number): Promise<languages.Location[]> {

        const scriptId = EditorUtil.getScriptId(model);

        const response = await omnisharpService.findUsages(scriptId, new api.FindUsagesRequest({
            onlyThisFile: true,
            excludeDefinition: true,
            line: lineNumber,
            column: column,
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
