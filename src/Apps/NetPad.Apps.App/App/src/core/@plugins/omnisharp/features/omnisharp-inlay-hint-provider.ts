import {CancellationToken, editor, Emitter, IEvent, languages, Range as MonacoRange} from "monaco-editor";
import {MonacoEditorUtil, IInlayHintsProvider} from "@application";
import * as api from "../api";
import {Converter} from "../utils";
import {FeatureProvider} from "./feature-provider";

export class OmniSharpInlayHintProvider extends FeatureProvider implements IInlayHintsProvider {
    private inlayHintsMap?: Map<languages.InlayHint, { model: editor.ITextModel, omnisharpHint: api.InlayHint }>;
    private _onDidChangeInlayHints: Emitter<void>;

    public displayName: string;
    public onDidChangeInlayHints: IEvent<void>;


    constructor() {
        super();
        this.displayName = "OmniSharp InlayHint Provider";
        this._onDidChangeInlayHints = new Emitter<void>();
        this.onDidChangeInlayHints = this._onDidChangeInlayHints.event;

        // In the future, if we support calls across different script files, we can call
        // this._onDidChangeInlayHints.fire() when text changes in any opened script to
        // recalculate inlay hints
    }

    public async provideInlayHints(model: editor.ITextModel, range: MonacoRange, token: CancellationToken): Promise<languages.InlayHintList> {
        const scriptId = MonacoEditorUtil.getScriptId(model);

        const response = await this.omnisharpService.getInlayHints(scriptId, new api.InlayHintRequest({
            location: new api.Location({
                fileName: "",
                range: Converter.monacoRangeToApiRange(range)
            })
        }), this.getAbortSignal(token));

        if (!response || !response.inlayHints) {
            return {
                hints: [],
                dispose: () => {
                    // do nothing
                }
            };
        }

        const inlayHintsMap = new Map<languages.InlayHint, { model: editor.ITextModel, omnisharpHint: api.InlayHint }>();

        const hints = response.inlayHints.map(inlayHint => {
            const mappedHint = this.toMonacoInlayHint(inlayHint);
            inlayHintsMap.set(mappedHint, {model: model, omnisharpHint: inlayHint});
            return mappedHint;
        });

        this.inlayHintsMap = inlayHintsMap;

        return {
            hints: hints,
            dispose: () => {
                // do nothing
            }
        };
    }

    public async resolveInlayHint(hint: languages.InlayHint, token: CancellationToken): Promise<languages.InlayHint> {
        if (!this.inlayHintsMap) return hint;

        if (!this.inlayHintsMap.has(hint)) {
            return Promise.reject("Outdated inlay hint was requested to be resolved, aborting.");
        }

        const entry = this.inlayHintsMap.get(hint);
        if (!entry) return hint;

        const scriptId = MonacoEditorUtil.getScriptId(entry.model);
        const omnisharpHint = entry.omnisharpHint;

        const response = await this.omnisharpService.resolveInlayHint(scriptId, new api.InlayHintResolveRequest({
            hint: new api.InlayHint2({
                tooltip: omnisharpHint.tooltip,
                position: Converter.monacoIPositionToApiPoint(hint.position),
                label: hint.label as string,
                data: new api.InlayHintData({
                    item1: omnisharpHint.data.item1!,
                    item2: omnisharpHint.data.item2
                })
            })
        }), this.getAbortSignal(token));

        if (!response) {
            return hint;
        }

        hint.tooltip = {
            value: response.tooltip || "",
            supportHtml: true,
            supportThemeIcons: true,
            isTrusted: true
        };

        return hint;
    }

    private toMonacoInlayHint(inlayHint: api.InlayHint): languages.InlayHint {
        return {
            label: inlayHint.label?.trim(),
            position: Converter.apiPointToMonacoIPosition(inlayHint.position),
            tooltip: {
                value: inlayHint.tooltip || "",
                supportHtml: true,
                supportThemeIcons: true,
                isTrusted: true
            },
            kind: languages.InlayHintKind.Type,
            textEdits: inlayHint.textEdits?.map(Converter.apiLinePositionSpanTextChangeToMonacoTextEdit)
        };
    }
}
