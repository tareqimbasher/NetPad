import {bindable, PLATFORM, watch} from "aurelia";
import {
    IEventBus,
    IScriptService,
    ISession,
    IShortcutManager,
    Script,
    ScriptEnvironment,
    ScriptKind,
    ScriptOutputEmitted,
    Settings
} from "@domain";
import * as monaco from "monaco-editor";
import Split from "split.js";
import {Util} from "@common";
import {ResultsViewSettings} from "./results-view-settings";

export class ScriptEnvironmentView {
    @bindable public environment: ScriptEnvironment;
    public resultsViewSettings: ResultsViewSettings;

    private disposables: (() => void)[] = [];
    private editor: monaco.editor.IStandaloneCodeEditor;
    private resultsEl: HTMLElement;

    constructor(
        readonly settings: Settings,
        @IScriptService readonly scriptService: IScriptService,
        @ISession readonly session: ISession,
        @IShortcutManager readonly shortcutManager: IShortcutManager,
        @IEventBus readonly eventBus: IEventBus) {
        this.resultsViewSettings = new ResultsViewSettings();
    }

    public get id(): string {
        return this.environment.script.id;
    }

    public get script(): Script {
        return this.environment.script;
    }

    public get kind(): ScriptKind {
        return this.script.config.kind;
    }

    public set kind(value) {
        this.scriptService.setScriptKind(this.script.id, value);
    }

    private attached() {
        this.kind = this.script.config.kind;

        const token = this.eventBus.subscribeToServer(ScriptOutputEmitted, msg => {
            if (msg.scriptId === this.environment.script.id) {
                this.appendResults(msg.output);
            }
        });

        this.disposables.push(() => token.dispose());

        PLATFORM.taskQueue.queueTask(() => {
            this.initializeEditor();
        }, { delay: 100 });

        const split = Split([
            `script-environment-view[data-id="${this.id}"] .text-editor-container`,
            `script-environment-view[data-id="${this.id}"] .results-container`
        ], {
            gutterSize: 6,
            direction: 'vertical',
            sizes: [50, 50],
            minSize: [50, 50],
        });

        this.disposables.push(() => split.destroy());
    }

    public detaching() {
        this.editor.dispose();
        for (const disposable of this.disposables) {
            disposable();
        }
    }

    public async run() {
        this.setResults(null);
        this.resultsViewSettings.show = true;
        await this.scriptService.run(this.environment.script.id);
    }

    private setResults(results: string | null) {
        if (!results)
            results = "";

        this.resultsEl.innerHTML = results
            .replaceAll("\n", "<br/>")
            .replaceAll(" ", "&nbsp;");
    }

    private appendResults(results: string | null) {
        this.setResults(this.resultsEl.innerHTML + results);
    }

    private initializeEditor() {
        const el = document.querySelector(`[data-text-editor-id="${this.id}"]`) as HTMLElement;
        this.editor = monaco.editor.create(el, {
            value: this.environment.script.code,
            language: 'csharp'
        });
        this.updateEditorTheme();

        monaco.languages.registerCompletionItemProvider("csharp", {
            provideCompletionItems: (model, position, ctx, token) => {
                return <any>{
                    suggestions: [
                        {
                            label: "Console.WriteLine",
                            kind: monaco.languages.CompletionItemKind.Snippet,
                            documentation: "Write a line to the console",
                            inertText: "Console.WriteLine();",
                            range: {
                                replace: {
                                    startLineNumber: position.lineNumber,
                                    endLineNumber: position.lineNumber,
                                    startColumn: position.column,
                                    endColumn: position.column
                                }
                            }
                        }
                    ],
                    incomplete: false
                };
            }
        })

        const f = Util.debounce(this, async (ev) => {
            await this.scriptService.updateCode(this.environment.script.id, this.editor.getValue());
        }, 500, true);

        this.editor.onDidChangeModelContent(ev => f(ev));

        const ob = new ResizeObserver(entries => this.updateEditorLayout());
        ob.observe(document.getElementById("sidebar"));
        ob.observe(document.querySelector(`script-environment-view[data-id="${this.id}"] .text-editor-container`));
        ob.observe(document.querySelector(`script-environment-view[data-id="${this.id}"] .results-container`));
        this.disposables.push(() => ob.disconnect());
    }

    @watch<ScriptEnvironmentView>(vm => vm.session.active)
    private activeScriptEnvironmentChanged() {
        PLATFORM.taskQueue.queueTask(() => {
            if (this.environment === this.session.active)
                this.updateEditorLayout();
        }, { delay: 100 });
    }

    private updateEditorLayout() {
        this.editor.layout();
    }

    @watch<ScriptEnvironmentView>(vm => vm.settings.theme)
    private updateEditorTheme() {
        monaco.editor.setTheme(this.settings.theme === "Light" ? "vs" : "vs-dark");
    }
}
