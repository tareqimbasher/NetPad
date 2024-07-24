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
import {KeyCodeNum} from "@common";
import {IEventBus, Settings, SettingsUpdatedEvent} from "@application";
import {ShortcutIds} from "@application/shortcuts/builtin-shortcuts";
import {MonacoThemeManager} from "./monaco-theme-manager";

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
        this.registerKeyboardShortcuts();
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

    private static registerKeyboardShortcuts() {
        // Currently we are only overriding the Command Palette keybinding.
        let commandPaletteKeybinding: number;

        const addOrUpdateShortcuts = (settings: Settings) => {
            const commandPaletteShortcutConfig = this.settings.keyboardShortcuts.shortcuts
                .find(s => s.id === ShortcutIds.openCommandPalette);

            // If the config for this shortcut doesn't exist yet, or did but is now removed.
            if (!commandPaletteShortcutConfig) {
                if (commandPaletteKeybinding) {
                    // Disable previous rule
                    monaco.editor.addKeybindingRule({
                        keybinding: commandPaletteKeybinding,
                        command: null,
                    });
                }

                monaco.editor.addKeybindingRule({
                    keybinding: monaco.KeyCode.F1,
                    command: "editor.action.quickCommand",
                });

                this.environmentInfo.keyboardShortcuts.push(monaco.KeyCode.F1);
                return;
            }

            if (commandPaletteKeybinding === undefined) {
                // If this is first time we are customizing the command palette keybinding,
                // disable default show command palette
                monaco.editor.addKeybindingRule({
                    keybinding: monaco.KeyCode.F1,
                    command: null,
                });
            } else {
                // Disable previous rule
                monaco.editor.addKeybindingRule({
                    keybinding: commandPaletteKeybinding,
                    command: null,
                });
            }

            // Add a new rule for the new keybinding
            const combo: number[] = [];
            if (commandPaletteShortcutConfig.meta) combo.push(monaco.KeyMod.WinCtrl);
            if (commandPaletteShortcutConfig.alt) combo.push(monaco.KeyMod.Alt);
            if (commandPaletteShortcutConfig.ctrl) combo.push(monaco.KeyMod.CtrlCmd);
            if (commandPaletteShortcutConfig.shift) combo.push(monaco.KeyMod.Shift);
            if (commandPaletteShortcutConfig.key) combo.push(KeyCodeNum[commandPaletteShortcutConfig.key!.toString() as keyof typeof KeyCodeNum]);

            commandPaletteKeybinding = combo.reduce((a, b) => a | b, 0);

            monaco.editor.addKeybindingRule({
                keybinding: commandPaletteKeybinding,
                command: "editor.action.quickCommand",
            });

            this.environmentInfo.keyboardShortcuts.push(commandPaletteKeybinding);
        };

        this.eventBus.subscribeToServer(SettingsUpdatedEvent, event => addOrUpdateShortcuts(event.settings));

        addOrUpdateShortcuts(this.settings);
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
