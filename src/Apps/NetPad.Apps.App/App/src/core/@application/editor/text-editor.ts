import {DI, ILogger} from "aurelia";
import {watch} from "@aurelia/runtime-html";
import * as monaco from "monaco-editor";
import {WithDisposables} from "@common";
import {IEventBus, MonacoEditorUtil, Settings, ViewModelBase} from "@application";
import {AppTheme} from "@application/themes/app-theme";
import {TextDocument} from "./text-document";
import {ITextEditorService} from "./itext-editor-service";

export const ITextEditor = DI.createInterface<ITextEditor>();

export interface ITextEditor extends WithDisposables {
    monaco: monaco.editor.IStandaloneCodeEditor;
    position?: monaco.Position | null;
    active?: TextDocument | null;

    open(document: TextDocument): void;
    close(documentId: string): void;
    focus(): void;
}

export class TextEditor extends ViewModelBase implements ITextEditor {
    public monaco: monaco.editor.IStandaloneCodeEditor;
    public position?: monaco.Position | null;
    public active?: TextDocument | null;

    private monacoEditorElement: HTMLElement;
    private viewStates = new Map<string, monaco.editor.ICodeEditorViewState | null>();

    constructor(
        private readonly settings: Settings,
        @ITextEditorService private readonly textEditorService: ITextEditorService,
        @IEventBus private readonly eventBus: IEventBus,
        @ILogger logger: ILogger) {
        super(logger);
    }

    public open(document: TextDocument) {
        this.ensureEditorInitialized();

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
        this.ensureEditorInitialized();

        this.viewStates.delete(documentId);
        if (this.active && this.active.id === documentId) {
            this.monaco.setModel(null);
            this.active = null;
        }
    }

    public focus() {
        setTimeout(() => this.monaco.focus(), 50);
    }

    private ensureEditorInitialized() {
        if (this.monaco) return;
        this.initializeEditor();
    }

    private initializeEditor() {
        if (this.monaco) return;

        this.monaco = monaco.editor.create(this.monacoEditorElement, {
            model: null,
            "semanticHighlighting.enabled": true,
            formatOnType: true,
            formatOnPaste: true,
            automaticLayout: true,
        });

        this.updateEditorSettings();

        this.addDisposable(this.monaco.onDidFocusEditorText(() => {
            if (this.textEditorService.active !== this) {
                this.textEditorService.active = this;
            }
        }));

        this.addDisposable(this.monaco.onDidDispose(() => {
            if (this.textEditorService.active === this) {
                this.textEditorService.active = undefined;
            }
        }));

        this.addDisposable(
            this.monaco.onDidChangeCursorSelection(ev => {
                if (this.active) {
                    this.active.selection = ev.selection;
                }
            })
        );

        this.addDisposable(
            this.monaco.onDidChangeCursorPosition(ev => this.position = ev.position)
        );

        this.addDisposable(
            this.monaco.onDidChangeModel(ev => {
                this.position = this.monaco.getPosition();
            })
        );

        // A machine light/dark flip changes no setting, so the watchers below never see it. In
        // System mode the editor has to be told directly.
        this.addDisposable(AppTheme.onSystemGroundChanged(() => {
            if (this.settings.appearance.mode === "System") void this.updateEditorSettings();
        }));

        // Defer grabbing current position, getting position directly after editor creation is unreliable
        setTimeout(() => this.position = this.monaco.getPosition());

        this.focus();

        this.addDisposable(() => {
            this.viewStates.clear();
            this.monaco.dispose();
        });
    }

    @watch<TextEditor>(vm => vm.settings.appearance.themeFamily)
    @watch<TextEditor>(vm => vm.settings.appearance.mode)
    @watch<TextEditor>(vm => vm.settings.appearance.background)
    @watch<TextEditor>(vm => vm.settings.editor.monacoOptions)
    private async updateEditorSettings() {
        if (!this.monaco) return;
        await MonacoEditorUtil.updateOptions(this.monaco, this.settings);
    }
}
