import {CancellationToken, editor, Emitter, IEvent, languages, Range} from "monaco-editor";
import {
    IDocumentRangeSemanticTokensProvider,
    IDocumentSemanticTokensProvider,
    IEventBus,
    MonacoEditorUtil,
    ScriptConfigPropertyChangedEvent
} from "@application";
import * as api from "../api";
import {Converter} from "../utils";
import {SemanticTokens} from "../types";
import {FeatureProvider} from "./feature-provider";

export class OmniSharpSemanticTokensProvider extends FeatureProvider implements IDocumentSemanticTokensProvider, IDocumentRangeSemanticTokensProvider {
    private readonly legend: languages.SemanticTokensLegend;
    private _onDidChange: Emitter<void>;

    public onDidChange: IEvent<void>;

    constructor(@IEventBus private readonly eventBus: IEventBus) {
        super();
        this.legend = {
            tokenTypes: SemanticTokens.tokenTypes,
            tokenModifiers: SemanticTokens.tokenModifiers
        };

        this._onDidChange = new Emitter<void>();
        this.onDidChange = this._onDidChange.event;

        this.eventBus.subscribeToServer(ScriptConfigPropertyChangedEvent, message => {
            if (message.propertyName === "Namespaces") {
                this._onDidChange.fire();
            }
        });

        this.eventBus.subscribeToServer(api.OmniSharpAsyncBufferUpdateCompletedEvent, message => {
            setTimeout(() => this._onDidChange.fire(), 500);
        });
    }

    public getLegend(): languages.SemanticTokensLegend {
        return this.legend;
    }

    public provideDocumentSemanticTokens(model: editor.ITextModel, lastResultId: string | null, token: CancellationToken): languages.ProviderResult<languages.SemanticTokens | languages.SemanticTokensEdits> {
        return this.provideSemanticTokens(model, null, token);
    }

    public provideDocumentRangeSemanticTokens(model: editor.ITextModel, range: Range, token: CancellationToken): languages.ProviderResult<languages.SemanticTokens> {
        return this.provideSemanticTokens(model, range, token);
    }

    public releaseDocumentSemanticTokens(resultId: string | undefined): void {
        // do nothing
    }

    private async provideSemanticTokens(model: editor.ITextModel, range: Range | null | undefined, token: CancellationToken) {

        const scriptId = MonacoEditorUtil.getScriptId(model);

        const request = new api.SemanticHighlightRequest();
        if (range) {
            request.range = Converter.monacoRangeToApiRange(range);
        }

        if (token.isCancellationRequested) {
            return null;
        }

        // We don't need to retreive semantic info if there is no code in the editor
        if (model.getValue().trim().length == 0) {
            return null;
        }

        const versionBeforeRequest = model.getVersionId();

        const response = await this.omnisharpService.getSemanticHighlights(scriptId, request, this.getAbortSignal(token));

        if (model.isDisposed()) { // Can happen if model (ie. script tab) is closed before the response is returned
            return null;
        }

        const versionAfterRequest = model.getVersionId();

        if (versionBeforeRequest !== versionAfterRequest) {
            // Cannot convert result's offsets to (line;col) values correctly
            // a new request will come in soon...
            //
            // Here we cannot return null, because returning null would remove all semantic tokens.
            // We must throw to indicate that the semantic tokens should not be removed.
            throw new Error("busy");
        }

        if (token.isCancellationRequested || !response || !response.spans) {
            return null;
        }

        return this.processResponse(response, model);
    }

    private processResponse(response: api.SemanticHighlightResponse, model: editor.ITextModel): languages.ProviderResult<languages.SemanticTokens> {
        if (!response.spans) {
            return {
                data: new Uint32Array(),
                resultId: undefined
            };
        }

        const data: number[] = [];

        let prevLine = 0;
        let prevChar = 0;

        const createDeltaEncodedTokenData = (lineNumber: number, colPosition: number, length: number, tokenTypeIndex: number, modifierIndex: number) => {
            // Convert to 0-indexed
            lineNumber--;
            colPosition--;

            const arr = [
                // Line number (0-indexed, and offset from the previous line)
                lineNumber - prevLine,
                // Column position (0-indexed, and offset from the previous column, unless this is the beginning of a new line)
                prevLine === lineNumber ? colPosition - prevChar : colPosition,
                // Token length
                length,
                tokenTypeIndex,
                modifierIndex
            ];

            prevLine = lineNumber
            prevChar = colPosition

            return arr;
        };

        for (const span of response.spans) {
            const tokenType = SemanticTokens.tokenTypeMap[SemanticTokens.SemanticHighlightClassification[span.type]];
            if (tokenType === undefined) {
                continue;
            }

            let tokenModifiers = span.modifiers?.reduce((modifiers, modifier) =>
                modifiers + SemanticTokens.tokenModifierMap[SemanticTokens.SemanticHighlightModifier[modifier]], 0) || 0;

            // We could add a separate classification for constants but they are
            // supported as a readonly variable. Until we start getting more complete
            // modifiers from the highlight service we can add the readonly modifier here.
            if (span.type === SemanticTokens.SemanticHighlightClassification[SemanticTokens.SemanticHighlightClassification.ConstantName]) {
                tokenModifiers += 2 ** SemanticTokens.DefaultTokenModifier.readonly;
            }

            const spanRange = Converter.apiSemanticHighlightSpanToMonacoRange(span);
            const isMultiLineRange = spanRange.startLineNumber !== spanRange.endLineNumber;

            for (let iLine = spanRange.startLineNumber; iLine <= spanRange.endLineNumber; iLine++) {
                // If we are on the "range.StartLineNumber", use the start column, otherwise we are in a range that
                // spans multiple lines, and the start column should be the first char since its a continuation
                // of the previous line
                const startColumn = iLine === spanRange.startLineNumber ? spanRange.startColumn : 1;

                let length = 0;

                if (!isMultiLineRange) {
                    length = spanRange.endColumn - spanRange.startColumn;
                } else {
                    // First line
                    if (iLine === spanRange.startLineNumber) {
                        length = model.getLineLength(iLine) - spanRange.startColumn;
                    }
                    // Line in the middle (not first or last line)
                    else if (iLine > spanRange.startLineNumber && iLine < spanRange.endLineNumber) {
                        length = model.getLineLength(iLine);
                    }
                    // Last line
                    else {
                        length = spanRange.endColumn;
                    }
                }

                const arr = createDeltaEncodedTokenData(
                    iLine,
                    startColumn,
                    length,
                    tokenType,
                    tokenModifiers);

                data.push(...arr);

                // console.debug(
                //     spanRange,
                //     span.type,
                //     tokenType,
                //     tokenTypes[tokenType],
                //     model.getValueInRange(new Range(iLine, startColumn, iLine, startColumn + length)),
                //     length);
                // console.debug(arr);
            }
        }

        return {
            data: new Uint32Array(data),
            resultId: undefined
        };
    }
}
