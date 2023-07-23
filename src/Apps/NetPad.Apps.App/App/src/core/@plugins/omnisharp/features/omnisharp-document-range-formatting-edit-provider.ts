import {CancellationToken, editor, languages, Range as MonacoRange} from "monaco-editor";
import {EditorUtil, IDocumentRangeFormattingEditProvider} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import {FormatRangeRequest} from "../api";
import {Converter} from "../utils";

export class OmnisharpDocumentRangeFormattingEditProvider implements IDocumentRangeFormattingEditProvider
{
    constructor(@IOmniSharpService private readonly omnisharpService: IOmniSharpService) {
    }

    public async provideDocumentRangeFormattingEdits(model: editor.ITextModel, range: MonacoRange, options: languages.FormattingOptions, token: CancellationToken): Promise<languages.TextEdit[]> {
        const scriptId = EditorUtil.getScriptId(model);

        const response = await this.omnisharpService.formatRange(scriptId, new FormatRangeRequest({
            line: range.startLineNumber,
            column: range.startColumn,
            endLine: range.endLineNumber,
            endColumn: range.endColumn,
            applyChangesTogether: false
        }));

        if (!response || !response.changes || !response.changes.length) {
            return [];
        }

        return response.changes.map(Converter.apiLinePositionSpanTextChangeToMonacoTextEdit);
    }
}
