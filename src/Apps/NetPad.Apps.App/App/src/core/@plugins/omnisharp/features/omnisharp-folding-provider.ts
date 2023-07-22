import {CancellationToken, editor, Emitter, IEvent, languages} from "monaco-editor";
import {EditorUtil, IFoldingRangeProvider} from "@application";
import {IOmniSharpService} from "../omnisharp-service";
import * as api from "../api";
import {IEventBus} from "@domain";

export class OmnisharpFoldingProvider implements IFoldingRangeProvider {
    private _onDidChange: Emitter<this>;

    public onDidChange: IEvent<this>;

    constructor(@IOmniSharpService private readonly omnisharpService: IOmniSharpService,
                @IEventBus private readonly eventBus: IEventBus) {
        this._onDidChange = new Emitter<this>();
        this.onDidChange = this._onDidChange.event;

        this.eventBus.subscribeToServer(api.OmniSharpAsyncBufferUpdateCompletedEvent, message => {
            setTimeout(() => this._onDidChange.fire(this), 500);
        });
    }

    public async provideFoldingRanges(model: editor.ITextModel, context: languages.FoldingContext, token: CancellationToken): Promise<languages.FoldingRange[]> {
        const scriptId = EditorUtil.getScriptId(model);

        const response = await this.omnisharpService.getBlockStructure(scriptId, new AbortController().signalFrom(token));

        if (!response || !response.spans || !response.spans.length) {
            return [];
        }

        const foldingRanges: languages.FoldingRange[] = [];

        for (const span of response.spans) {
            if (!span.range || span.range.start === undefined || span.range.end === undefined) {
                continue;
            }

            foldingRanges.push({
                start: span.range?.start?.line,
                end: span.range?.end?.line,
                kind: this.getType(span.kind)
            })
        }

        return foldingRanges;
    }

    private getType(type?: string): languages.FoldingRangeKind | undefined {
        switch (type) {
            case "Comment":
                return languages.FoldingRangeKind.Comment;
            case "Imports":
                return languages.FoldingRangeKind.Imports;
            case "Region":
                return languages.FoldingRangeKind.Region;
            default:
                return undefined;
        }
    }
}
