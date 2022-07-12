import {CancellationToken, editor, languages, Position, Range} from "monaco-editor";
import {EditorUtil} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import {FindUsagesRequest} from "../api";

export class OmnisharpReferenceProvider implements languages.ReferenceProvider {
    constructor(@IOmniSharpService private readonly omnisharpService: IOmniSharpService) {
    }

    public async provideReferences(model: editor.ITextModel, position: Position, context: languages.ReferenceContext, token: CancellationToken)
        : Promise<languages.Location[]> {

        return await OmnisharpReferenceProvider.findUsages(model, this.omnisharpService, position.lineNumber, position.column);
    }

    public static async findUsages(
        model: editor.ITextModel,
        omnisharpService: IOmniSharpService,
        lineNumber: number,
        column: number): Promise<languages.Location[]> {

        const scriptId = EditorUtil.getScriptId(model);

        const response = await omnisharpService.findUsages(scriptId, new FindUsagesRequest({
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
                range: new Range(qf.line, qf.column + 1, qf.endLine, qf.endColumn)
            }
        });
    }
}
