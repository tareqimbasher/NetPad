import {bindable, PLATFORM, watch} from "aurelia";
import {
    IEventBus,
    IScriptService,
    ISession,
    IShortcutManager,
    RunScriptEvent,
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
    public running = false;
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

        let token = this.eventBus.subscribe(RunScriptEvent, async msg => {
            if ((msg.scriptId && msg.scriptId === this.script.id) ||
                (!msg.scriptId && this.session.active.script.id === this.script.id)) {
                await this.run();
            }
        });
        this.disposables.push(() => token.dispose());

        token = this.eventBus.subscribeToServer(ScriptOutputEmitted, msg => {
            if (msg.scriptId === this.script.id) {
                this.appendResults(msg.output);
            }
        });
        this.disposables.push(() => token.dispose());

        PLATFORM.taskQueue.queueTask(() => {
            this.initializeEditor();
        }, {delay: 100});

        const split = Split([
            `script-environment-view[data-id="${this.script.id}"] .text-editor-container`,
            `script-environment-view[data-id="${this.script.id}"] .results-container`
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

    private async sendCodeToServer() {
        await this.scriptService.updateCode(this.script.id, this.editor.getValue());
    }

    public async run() {
        if (this.running) return;

        this.running = true;

        try {
            await this.sendCodeToServer();
            this.setResults(null);
            this.resultsViewSettings.show = true;
            await this.scriptService.run(this.script.id);
        } finally {
            this.running = false;
        }
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
        const el = document.querySelector(`[data-text-editor-id="${this.script.id}"]`) as HTMLElement;
        this.editor = monaco.editor.create(el, {
            value: this.script.code,
            language: 'csharp'
        });
        this.updateEditorSettings();

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
            await this.sendCodeToServer();
        }, 500, true);

        this.editor.onDidChangeModelContent(ev => f(ev));

        const ob = new ResizeObserver(entries => this.updateEditorLayout());
        ob.observe(document.getElementById("sidebar"));
        ob.observe(document.querySelector(`script-environment-view[data-id="${this.script.id}"] .text-editor-container`));
        ob.observe(document.querySelector(`script-environment-view[data-id="${this.script.id}"] .results-container`));
        this.disposables.push(() => ob.disconnect());
    }

    @watch<ScriptEnvironmentView>(vm => vm.session.active)
    private activeScriptEnvironmentChanged() {
        PLATFORM.taskQueue.queueTask(() => {
            if (this.environment === this.session.active)
                this.updateEditorLayout();
        }, {delay: 100});
    }

    private updateEditorLayout() {
        this.editor.layout();
    }

    @watch<ScriptEnvironmentView>(vm => vm.settings.theme)
    @watch<ScriptEnvironmentView>(vm => vm.settings.editorBackgroundColor)
    @watch<ScriptEnvironmentView>(vm => vm.settings.editorOptions)
    private updateEditorSettings() {
        let theme = this.settings.theme === "Light" ? "vs" : "vs-dark";

        if (this.settings.editorBackgroundColor) {
            monaco.editor.defineTheme("custom-theme", {
                base: <any>theme,
                inherit: true,
                rules: [],
                colors: {
                    "editor.background": this.settings.editorBackgroundColor,
                },
            });
            theme = "custom-theme";
        }

        const options = {
            theme: theme
        };

        Object.assign(options, this.settings.editorOptions || {})
        this.editor.updateOptions(options);
    }
}
