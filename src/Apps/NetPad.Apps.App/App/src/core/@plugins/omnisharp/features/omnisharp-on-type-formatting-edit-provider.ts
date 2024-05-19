import {CancellationToken, editor, languages, Position} from "monaco-editor";
import {MonacoEditorUtil, IOnTypeFormattingEditProvider} from "@application";
import {FormatAfterKeystrokeRequest} from "../api";
import {Converter} from "../utils";
import {FeatureProvider} from "./feature-provider";

export class OmnisharpOnTypeFormattingEditProvider extends FeatureProvider implements IOnTypeFormattingEditProvider {
    public readonly autoFormatTriggerCharacters: string[] = ["}", "/", "\n", ";"];

    public async provideOnTypeFormattingEdits(model: editor.ITextModel, position: Position, ch: string, options: languages.FormattingOptions, token: CancellationToken): Promise<languages.TextEdit[]> {
        const scriptId = MonacoEditorUtil.getScriptId(model);

        const request = new FormatAfterKeystrokeRequest();
        request.line = position.lineNumber;
        request.column = position.column;
        request.character = ch;

        const response = await this.omnisharpService.formatAfterKeystroke(scriptId, request);

        if (!response || !response.changes || !response.changes.length) {
            return [];
        }

        return response.changes.map(Converter.apiLinePositionSpanTextChangeToMonacoTextEdit);
    }
}
