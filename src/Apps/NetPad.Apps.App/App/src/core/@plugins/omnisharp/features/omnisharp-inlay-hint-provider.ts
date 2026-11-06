import {CancellationToken, editor, Emitter, IEvent, languages, Range as MonacoRange} from "monaco-editor";
import {IEventBus, IInlayHintsProvider, MonacoEditorUtil} from "@application";
import * as api from "../api";
import {Converter} from "../utils";
import {FeatureProvider} from "./feature-provider";
import {onServerReadyForShownScript} from "./common";

export class OmniSharpInlayHintProvider extends FeatureProvider implements IInlayHintsProvider {
    private inlayHintsMap?: Map<languages.InlayHint, { model: editor.ITextModel, omnisharpHint: api.InlayHint }>;
    private _onDidChangeInlayHints: Emitter<void>;

    public displayName: string;
    public onDidChangeInlayHints: IEvent<void>;


    constructor(@IEventBus eventBus: IEventBus) {
        super();
        this.displayName = "OmniSharp InlayHint Provider";
        this._onDidChangeInlayHints = new Emitter<void>();
        this.onDidChangeInlayHints = this._onDidChangeInlayHints.event;

        onServerReadyForShownScript(eventBus, () => this.askEditorsAgain());

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

    /**
     * Firing onDidChangeInlayHints is not enough: Monaco 0.39 listens to it only after this provider has
     * answered with at least one hint, so an editor whose first answer was empty would never ask again.
     * Adding and removing a provider makes every editor ask again.
     */
    private askEditorsAgain() {
        languages.registerInlayHintsProvider("csharp", {
            provideInlayHints: () => ({hints: [], dispose: () => { /* nothing to release */ }})
        }).dispose();
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
