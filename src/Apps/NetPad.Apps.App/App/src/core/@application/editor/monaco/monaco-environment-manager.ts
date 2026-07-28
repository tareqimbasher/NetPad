import {IContainer, ILogger} from "aurelia";
import * as monaco from "monaco-editor";
import {
    IActionProvider,
    ICodeActionProvider,
    ICodeLensProvider,
    ICommandProvider,
    ICompletionItemProvider,
    IDiagnosticsProvider,
    IDocumentHighlightProvider,
    IDocumentRangeFormattingEditProvider,
    IDocumentRangeSemanticTokensProvider,
    IDocumentSemanticTokensProvider,
    IDocumentSymbolProvider,
    IFoldingRangeProvider,
    IHoverProvider,
    IImplementationProvider,
    IInlayHintsProvider,
    IOnTypeFormattingEditProvider,
    IReferenceProvider,
    IRenameProvider,
    ISignatureHelpProvider
} from "../providers/interfaces";
import {IEventBus, Settings, SettingsUpdatedEvent} from "@application";
import {resolveKeybindings} from "@application/keybindings/builtin-keybindings";
import {MonacoThemeManager} from "./monaco-theme-manager";
import {toMonacoKeybinding} from "./monaco-key-combo";

export class MonacoEnvironmentInfo {
    commands: unknown[] = [];
    actions: unknown[] = [];
    keyboardShortcuts: unknown[] = [];
    completionProviders: unknown[] = [];
    semanticTokensProviders: unknown[] = [];
    documentRangeSemanticTokensProvider: unknown[] = [];
    documentSymbolProviders: unknown[] = [];
    implementationProviders: unknown[] = [];
    hoverProviders: unknown[] = [];
    signatureHelpProviders: unknown[] = [];
    referenceProviders: unknown[] = [];
    documentHighlightProviders: unknown[] = [];
    codeLensProviders: unknown[] = [];
    inlayHintsProviders: unknown[] = [];
    codeActionProviders: unknown[] = [];
    foldingRangeProviders: unknown[] = [];
    documentRangeFormattingEditProviders: unknown[] = [];
    onTypeFormattingEditProviders: unknown[] = [];
    renameProviders: unknown[] = [];
    diagnosticsProviders: unknown[] = [];

    public clear() {
        for (const propName in this) {
            const value = this[propName as keyof typeof this];
            if (Array.isArray(value)) {
                value.splice(0);
            }
        }
    }
}

/**
 * Sets up the Monaco editor environment.
 */
export class MonacoEnvironmentManager {
    private static container: IContainer;
    private static settings: Settings;
    private static eventBus: IEventBus;
    private static environmentInfo = new MonacoEnvironmentInfo();

    public static async setupMonacoEnvironment(container: IContainer) {
        if (this.container) {
            throw new Error("Monaco Environment is already initialized");
        }

        this.container = container;
        this.settings = container.get(Settings);
        this.eventBus = container.get(IEventBus);
        const logger = container.get(ILogger).scopeTo(nameof(MonacoEnvironmentManager));

        this.registerCommands();
        this.registerActions();
        this.shadowAppKeybindings();
        this.registerCompletionProviders();
        this.registerSemanticTokensProviders();
        this.registerDocumentSymbolProviders();
        this.registerImplementationProviders();
        this.registerHoverProviders();
        this.registerSignatureHelpProviders();
        this.registerReferenceProviders();
        this.registerDocumentHighlightProviders();
        this.registerCodeLensProviders();
        this.registerInlayHintsProviders();
        this.registerCodeActionProviders();
        this.registerFoldingRangeProviders();
        this.registerDocumentRangeFormattingEditProviders();
        this.registerOnTypeFormattingEditProviders();
        this.registerRenameProviders();
        this.registerDiagnosticsProviders();

        await MonacoThemeManager.initialize(this.settings);

        logger.debug("Monaco environment initialized", this.environmentInfo);

        this.environmentInfo.clear();
    }

    private static registerCommands() {
        for (const commandProvider of this.container.getAll(ICommandProvider)) {
            for (const command of commandProvider.provideCommands()) {
                monaco.editor.registerCommand(command.id, command.handler);
                this.environmentInfo.commands.push(command.id);
            }
        }
    }

    private static registerActions() {
        monaco.editor.onDidCreateEditor(e => {
            const editor = e as monaco.editor.IStandaloneCodeEditor;

            // Check if editor is a IStandaloneCodeEditor
            if (!editor.addAction) {
                return;
            }

            setTimeout(() => {
                for (const actionProvider of this.container.getAll(IActionProvider)) {
                    for (const action of actionProvider.provideActions()) {
                        // If action is already registered, don't register again
                        if (editor.getAction(action.id)) {
                            continue;
                        }

                        editor.addAction(action);
                        this.environmentInfo.commands.push(action.id);
                    }
                }
            }, 100);
        });
    }

    /**
     * Clears every key combination the app has bound out of the editor, so those keys reach the app
     * even while the editor has focus. An editor default a user wants back is reclaimed by moving
     * the app command off that combination.
     */
    private static shadowAppKeybindings() {
        let rules: monaco.IDisposable | undefined;

        const apply = (settings: Settings) => {
            const keybindings = resolveKeybindings(settings)
                .map(k => toMonacoKeybinding(k.keyCombo))
                .filter((keybinding): keybinding is number => keybinding !== undefined);

            // Disposing releases the previous set, restoring the editor defaults they covered.
            rules?.dispose();
            rules = monaco.editor.addKeybindingRules(
                keybindings.map(keybinding => ({keybinding, command: null}))
            );

            this.environmentInfo.keyboardShortcuts.splice(0, this.environmentInfo.keyboardShortcuts.length, ...keybindings);
        };

        this.eventBus.subscribeToServer(SettingsUpdatedEvent, event => apply(event.settings));

        apply(this.settings);
    }

    private static registerCompletionProviders() {
        for (const completionItemProvider of this.container.getAll(ICompletionItemProvider)) {
            monaco.languages.registerCompletionItemProvider(completionItemProvider.language, completionItemProvider);
            this.environmentInfo.completionProviders.push(completionItemProvider);
        }
    }

    private static registerSemanticTokensProviders() {
        for (const documentSemanticTokensProvider of this.container.getAll(IDocumentSemanticTokensProvider)) {
            monaco.languages.registerDocumentSemanticTokensProvider("csharp", documentSemanticTokensProvider);
            this.environmentInfo.semanticTokensProviders.push(documentSemanticTokensProvider);
        }

        for (const documentRangeSemanticTokensProvider of this.container.getAll(IDocumentRangeSemanticTokensProvider)) {
            monaco.languages.registerDocumentRangeSemanticTokensProvider("csharp", documentRangeSemanticTokensProvider);
            this.environmentInfo.documentRangeSemanticTokensProvider.push(documentRangeSemanticTokensProvider);
        }
    }

    private static registerDocumentSymbolProviders() {
        for (const documentSymbolProvider of this.container.getAll(IDocumentSymbolProvider)) {
            monaco.languages.registerDocumentSymbolProvider("csharp", documentSymbolProvider);
            this.environmentInfo.documentSymbolProviders.push(documentSymbolProvider);
        }
    }

    private static registerImplementationProviders() {
        for (const implementationProvider of this.container.getAll(IImplementationProvider)) {
            monaco.languages.registerImplementationProvider("csharp", implementationProvider);
            this.environmentInfo.implementationProviders.push(implementationProvider);
        }
    }

    private static registerHoverProviders() {
        for (const hoverProvider of this.container.getAll(IHoverProvider)) {
            monaco.languages.registerHoverProvider("csharp", hoverProvider);
            this.environmentInfo.hoverProviders.push(hoverProvider);
        }
    }

    private static registerSignatureHelpProviders() {
        for (const signatureHelpProvider of this.container.getAll(ISignatureHelpProvider)) {
            monaco.languages.registerSignatureHelpProvider("csharp", signatureHelpProvider);
            this.environmentInfo.signatureHelpProviders.push(signatureHelpProvider);
        }
    }

    private static registerReferenceProviders() {
        for (const referenceProvider of this.container.getAll(IReferenceProvider)) {
            monaco.languages.registerReferenceProvider("csharp", referenceProvider);
            this.environmentInfo.referenceProviders.push(referenceProvider);
        }
    }

    private static registerDocumentHighlightProviders() {
        for (const documentHighlightProvider of this.container.getAll(IDocumentHighlightProvider)) {
            monaco.languages.registerDocumentHighlightProvider("csharp", documentHighlightProvider);
            this.environmentInfo.documentHighlightProviders.push(documentHighlightProvider);
        }
    }

    private static registerCodeLensProviders() {
        for (const codeLensProvider of this.container.getAll(ICodeLensProvider)) {
            monaco.languages.registerCodeLensProvider("csharp", codeLensProvider);
            this.environmentInfo.codeLensProviders.push(codeLensProvider);
        }
    }

    private static registerInlayHintsProviders() {
        for (const inlayHintsProvider of this.container.getAll(IInlayHintsProvider)) {
            monaco.languages.registerInlayHintsProvider("csharp", inlayHintsProvider);
            this.environmentInfo.inlayHintsProviders.push(inlayHintsProvider);
        }
    }

    private static registerCodeActionProviders() {
        for (const codeActionProvider of this.container.getAll(ICodeActionProvider)) {
            monaco.languages.registerCodeActionProvider("csharp", codeActionProvider);
            this.environmentInfo.codeActionProviders.push(codeActionProvider);
        }
    }

    private static registerFoldingRangeProviders() {
        for (const foldingRangeProvider of this.container.getAll(IFoldingRangeProvider)) {
            monaco.languages.registerFoldingRangeProvider("csharp", foldingRangeProvider);
            this.environmentInfo.foldingRangeProviders.push(foldingRangeProvider);
        }
    }

    private static registerDocumentRangeFormattingEditProviders() {
        for (const documentRangeFormattingEditProvider of this.container.getAll(IDocumentRangeFormattingEditProvider)) {
            monaco.languages.registerDocumentRangeFormattingEditProvider("csharp", documentRangeFormattingEditProvider);
            this.environmentInfo.documentRangeFormattingEditProviders.push(documentRangeFormattingEditProvider);
        }
    }

    private static registerOnTypeFormattingEditProviders() {
        for (const onTypeFormattingEditProvider of this.container.getAll(IOnTypeFormattingEditProvider)) {
            monaco.languages.registerOnTypeFormattingEditProvider("csharp", onTypeFormattingEditProvider);
            this.environmentInfo.onTypeFormattingEditProviders.push(onTypeFormattingEditProvider);
        }
    }

    private static registerRenameProviders() {
        for (const renameProvider of this.container.getAll(IRenameProvider)) {
            monaco.languages.registerRenameProvider("csharp", renameProvider);
            this.environmentInfo.renameProviders.push(renameProvider);
        }
    }

    private static registerDiagnosticsProviders() {
        const provideDiagnostics = (model: monaco.editor.ITextModel) => {
            for (const diagnosticsProvider of this.container.getAll(IDiagnosticsProvider)) {
                diagnosticsProvider.provideDiagnostics(model, markers => {
                    monaco.editor.setModelMarkers(model, model.uri.toString(), markers);
                });
                this.environmentInfo.diagnosticsProviders.push(diagnosticsProvider);
            }
        };

        monaco.editor.onDidCreateModel(model => {
            if (model.getLanguageId() == "csharp") {
                provideDiagnostics(model);
            }

            model.onDidChangeLanguage(ev => {
                if (ev.newLanguage === "csharp") {
                    provideDiagnostics(model);
                } else {
                    monaco.editor.removeAllMarkers(model.uri.toString());
                }
            })
        });
    }
}
