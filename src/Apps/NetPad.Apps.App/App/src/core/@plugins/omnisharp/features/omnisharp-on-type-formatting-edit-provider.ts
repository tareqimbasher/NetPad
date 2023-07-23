import {CancellationToken, editor, languages, Position} from "monaco-editor";
import {EditorUtil, IOnTypeFormattingEditProvider} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import {FormatAfterKeystrokeRequest} from "../api";
import {Converter} from "../utils";

export class OmnisharpOnTypeFormattingEditProvider implements IOnTypeFormattingEditProvider {
    public readonly autoFormatTriggerCharacters: string[] = ["}", "/", "\n", ";"];

    constructor(@IOmniSharpService private readonly omnisharpService: IOmniSharpService) {
    }

    public async provideOnTypeFormattingEdits(model: editor.ITextModel, position: Position, ch: string, options: languages.FormattingOptions, token: CancellationToken): Promise<languages.TextEdit[]> {
        const scriptId = EditorUtil.getScriptId(model);

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
