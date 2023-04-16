import {ILogger} from "aurelia";
import * as monaco from "monaco-editor";
import {IEventBus, Settings} from "@domain";
import {EditorSetup, ViewModelBase} from "@application";
import {IDisposable} from "@common";
import {watch} from "@aurelia/runtime-html";
import {TextEditorFocusedEvent} from "@application/editor/events";
import {TextDocument} from "@application/editor/text-document";

export interface ITextEditor extends IDisposable {
    monaco: monaco.editor.IStandaloneCodeEditor;
    position?: monaco.Position | null;
    active?: TextDocument | null;

    open(document: TextDocument);

    close(documentId: string);

    focus();
}

export class TextEditor extends ViewModelBase implements ITextEditor {
    public monaco: monaco.editor.IStandaloneCodeEditor;
    public position?: monaco.Position | null;
    public active?: TextDocument | null;

    private viewStates = new Map<string, monaco.editor.ICodeEditorViewState | null>();

    constructor(
        readonly element: Element,
        readonly settings: Settings,
        @IEventBus private readonly eventBus: IEventBus,
        @ILogger logger: ILogger) {
        super(logger);
    }

    public async attached(): Promise<void> {
        this.ensureEditor();
        this.addDisposable(() => this.active = null);
    }

    public open(document: TextDocument) {
        this.ensureEditor();

        const currentOpen = this.active;
        if (currentOpen === document) {
            this.logger.warn(`Document is already open`);
            return;
        }

        if (currentOpen) {
            this.viewStates.set(currentOpen.id, this.monaco.saveViewState());
        }

        this.monaco.setModel(document.textModel);

        this.monaco.restoreViewState(this.viewStates.get(document.id) || null);

        this.active = document;
    }

    public close(documentId: string) {
        this.ensureEditor();

        this.viewStates.delete(documentId);
        if (this.active && this.active.id === documentId) {
            this.monaco.setModel(null);
            this.active = null;
        }
    }

    public focus() {
        setTimeout(() => this.monaco.focus(), 50);
    }

    private ensureEditor() {
        if (this.monaco) return;
        this.initializeEditor();
    }

    private initializeEditor() {
        if (this.monaco) return;

        this.monaco = monaco.editor.create(this.element as HTMLElement, {
            model: null,
            "semanticHighlighting.enabled": true
        });

        this.updateEditorSettings();

        this.addDisposable(this.monaco.onDidFocusEditorText(() => {
            this.eventBus.publish(new TextEditorFocusedEvent(this));
        }));

        this.addDisposable(
            this.monaco.onDidChangeCursorSelection(ev => {
                if (this.active) {
                    this.active.selection = ev.selection;
                }
            })
        );

        this.addDisposable(
            this.monaco.onDidChangeCursorPosition(ev => {
                this.position = ev.position;
            })
        );

        this.addDisposable(
            this.monaco.onDidChangeModel(ev => {
                this.position = this.monaco.getPosition();
            })
        );

        // Defer grabbing current position
        setTimeout(() => this.position = this.monaco.getPosition());

        this.focus();

        const ob = new ResizeObserver(() => this.updateEditorLayout());
        ob.observe(this.element);
        this.addDisposable(() => ob.disconnect());

        this.addDisposable(() => {
            this.viewStates.clear();
            this.monaco.dispose();
        });
    }

    private updateEditorLayout() {
        this.monaco.layout();
    }

    @watch<TextEditor>(vm => vm.settings.appearance.theme)
    @watch<TextEditor>(vm => vm.settings.editor.backgroundColor)
    @watch<TextEditor>(vm => vm.settings.editor.monacoOptions)
    private updateEditorSettings() {
        if (!this.monaco) return;

        let theme = this.settings.appearance.theme === "Light" ? "netpad-light-theme" : "netpad-dark-theme";

        if (this.settings.editor.backgroundColor) {
            const base: monaco.editor.BuiltinTheme = this.settings.appearance.theme === "Light" ? "vs" : "vs-dark";

            EditorSetup.defineTheme("custom-theme", {
                base: base,
                inherit: true,
                rules: [],
                colors: {
                    "editor.background": this.settings.editor.backgroundColor,
                },
            });
            theme = "custom-theme";
        }

        const options = {
            theme: theme
        };

        Object.assign(options, this.settings.editor.monacoOptions || {})
        this.monaco.updateOptions(options);
    }
}

export type TextLanguage =
    "plaintext"
    | "abap"
    | "apex"
    | "azcli"
    | "bat"
    | "bicep"
    | "cameligo"
    | "clojure"
    | "coffeescript"
    | "c"
    | "cpp"
    | "csharp"
    | "csp"
    | "css"
    | "cypher"
    | "dart"
    | "dockerfile"
    | "ecl"
    | "elixir"
    | "flow9"
    | "fsharp"
    | "freemarker2"
    | "freemarker2.tag-angle.interpolation-dollar"
    | "freemarker2.tag-bracket.interpolation-dollar"
    | "freemarker2.tag-angle.interpolation-bracket"
    | "freemarker2.tag-bracket.interpolation-bracket"
    | "freemarker2.tag-auto.interpolation-dollar"
    | "freemarker2.tag-auto.interpolation-bracket"
    | "go"
    | "graphql"
    | "handlebars"
    | "hcl"
    | "html"
    | "ini"
    | "java"
    | "javascript"
    | "julia"
    | "kotlin"
    | "less"
    | "lexon"
    | "lua"
    | "liquid"
    | "m3"
    | "markdown"
    | "mips"
    | "msdax"
    | "mysql"
    | "objective-c"
    | "pascal"
    | "pascaligo"
    | "perl"
    | "pgsql"
    | "php"
    | "pla"
    | "postiats"
    | "powerquery"
    | "powershell"
    | "proto"
    | "pug"
    | "python"
    | "qsharp"
    | "r"
    | "razor"
    | "redis"
    | "redshift"
    | "restructuredtext"
    | "ruby"
    | "rust"
    | "sb"
    | "scala"
    | "scheme"
    | "scss"
    | "shell"
    | "sol"
    | "aes"
    | "sparql"
    | "sql"
    | "st"
    | "swift"
    | "systemverilog"
    | "verilog"
    | "tcl"
    | "twig"
    | "typescript"
    | "vb"
    | "xml"
    | "yaml"
    | "json";
