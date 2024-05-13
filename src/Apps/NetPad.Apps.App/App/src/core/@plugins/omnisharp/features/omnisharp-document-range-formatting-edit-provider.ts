import {CancellationToken, editor, languages, Range as MonacoRange} from "monaco-editor";
import {MonacoEditorUtil, IDocumentRangeFormattingEditProvider} from "@application";
import {FormatRangeRequest} from "../api";
import {Converter} from "../utils";
import {FeatureProvider} from "./feature-provider";

export class OmnisharpDocumentRangeFormattingEditProvider extends FeatureProvider implements IDocumentRangeFormattingEditProvider {
    public async provideDocumentRangeFormattingEdits(model: editor.ITextModel, range: MonacoRange, options: languages.FormattingOptions, token: CancellationToken): Promise<languages.TextEdit[]> {
        const scriptId = MonacoEditorUtil.getScriptId(model);

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
