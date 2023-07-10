import {all} from "aurelia";
import * as monaco from "monaco-editor";
import {
    IActionProvider,
    ICodeActionProvider,
    ICodeLensProvider,
    ICommandProvider,
    ICompletionItemProvider,
    IDiagnosticsProvider,
    IDocumentHighlightProvider,
    IDocumentRangeSemanticTokensProvider,
    IDocumentSemanticTokensProvider,
    IHoverProvider,
    IImplementationProvider,
    IInlayHintsProvider,
    IReferenceProvider,
    ISignatureHelpProvider
} from "./providers/interfaces";

export class EditorSetup {
    constructor(
        @all(ICommandProvider) private readonly commandProviders: ICommandProvider[],
        @all(IActionProvider) private readonly actionProviders: IActionProvider[],
        @all(ICompletionItemProvider) private readonly completionItemProviders: ICompletionItemProvider[],
        @all(IDocumentSemanticTokensProvider) private readonly documentSemanticTokensProviders: IDocumentSemanticTokensProvider[],
        @all(IDocumentRangeSemanticTokensProvider) private readonly documentRangeSemanticTokensProviders: IDocumentRangeSemanticTokensProvider[],
        @all(IImplementationProvider) private readonly implementationProviders: IImplementationProvider[],
        @all(IHoverProvider) private readonly hoverProviders: IHoverProvider[],
        @all(ISignatureHelpProvider) private readonly signatureHelpProviders: ISignatureHelpProvider[],
        @all(IReferenceProvider) private readonly referenceProviders: IReferenceProvider[],
        @all(IDocumentHighlightProvider) private readonly documentHighlightProviders: IDocumentHighlightProvider[],
        @all(ICodeLensProvider) private readonly codeLensProviders: ICodeLensProvider[],
        @all(IInlayHintsProvider) private readonly inlayHintsProviders: IInlayHintsProvider[],
        @all(ICodeActionProvider) private readonly codeActionProviders: ICodeActionProvider[],
        @all(IDiagnosticsProvider) private readonly diagnosticsProviders: IDiagnosticsProvider[],
    ) {
    }

    public setup() {
        this.registerThemes();
        this.registerCommands();
        this.registerActions();
        this.registerCompletionProviders();
        this.registerSemanticTokensProviders();
        this.registerImplementationProviders();
        this.registerHoverProviders();
        this.registerSignatureHelpProviders();
        this.registerReferenceProviders();
        this.registerDocumentHighlightProviders();
        this.registerCodeLensProviders();
        this.registerInlayHintsProviders();
        this.registerCodeActionProviders();
        this.registerDiagnosticsProviders();
    }

    public static defineTheme(themeName: string, themeData: monaco.editor.IStandaloneThemeData) {
        if (!themeData.rules || !themeData.rules.length) {
            themeData.rules = themeData.base == "vs" ? this.lightThemeTokenThemeRules : this.darkThemeTokenThemeRules;
        }

        monaco.editor.defineTheme(themeName, themeData);
    }

    private registerThemes() {
        EditorSetup.defineTheme("netpad-light-theme", {
            base: "vs",
            inherit: true,
            rules: [],
            colors: {}
        });
        EditorSetup.defineTheme("netpad-dark-theme", {
            base: "vs-dark",
            inherit: true,
            rules: [],
            colors: {}
        });
    }

    private registerCommands() {
        for (const commandProvider of this.commandProviders) {
            for (const command of commandProvider.provideCommands()) {
                monaco.editor.registerCommand(command.id, command.handler);
            }
        }
    }

    private registerActions() {
        monaco.editor.onDidCreateEditor(e => {
            const editor = e as monaco.editor.IStandaloneCodeEditor;

            // Check if editor is a IStandaloneCodeEditor
            if (!editor.addAction) {
                return;
            }

            setTimeout(() => {
                for (const actionProvider of this.actionProviders) {
                    for (const action of actionProvider.provideActions()) {
                        // If action is already registered, don't register again
                        if (editor.getAction(action.id)) {
                            continue;
                        }

                        editor.addAction(action);
                    }
                }
            }, 100);
        });
    }

    private registerCompletionProviders() {
        for (const completionItemProvider of this.completionItemProviders) {
            monaco.languages.registerCompletionItemProvider(completionItemProvider.language, completionItemProvider);
        }
    }

    private registerSemanticTokensProviders() {
        for (const documentSemanticTokensProvider of this.documentSemanticTokensProviders) {
            monaco.languages.registerDocumentSemanticTokensProvider("csharp", documentSemanticTokensProvider);
        }

        for (const documentRangeSemanticTokensProvider of this.documentRangeSemanticTokensProviders) {
            monaco.languages.registerDocumentRangeSemanticTokensProvider("csharp", documentRangeSemanticTokensProvider);
        }
    }

    private registerImplementationProviders() {
        for (const implementationProvider of this.implementationProviders) {
            monaco.languages.registerImplementationProvider("csharp", implementationProvider);
        }
    }

    private registerHoverProviders() {
        for (const hoverProvider of this.hoverProviders) {
            monaco.languages.registerHoverProvider("csharp", hoverProvider);
        }
    }

    private registerSignatureHelpProviders() {
        for (const signatureHelpProvider of this.signatureHelpProviders) {
            monaco.languages.registerSignatureHelpProvider("csharp", signatureHelpProvider);
        }
    }

    private registerReferenceProviders() {
        for (const referenceProvider of this.referenceProviders) {
            monaco.languages.registerReferenceProvider("csharp", referenceProvider);
        }
    }

    private registerDocumentHighlightProviders() {
        for (const documentHighlightProvider of this.documentHighlightProviders) {
            monaco.languages.registerDocumentHighlightProvider("csharp", documentHighlightProvider);
        }
    }

    private registerCodeLensProviders() {
        for (const codeLensProvider of this.codeLensProviders) {
            monaco.languages.registerCodeLensProvider("csharp", codeLensProvider);
        }
    }

    private registerInlayHintsProviders() {
        for (const inlayHintsProvider of this.inlayHintsProviders) {
            monaco.languages.registerInlayHintsProvider("csharp", inlayHintsProvider);
        }
    }

    private registerCodeActionProviders() {
        for (const codeActionProvider of this.codeActionProviders) {
            monaco.languages.registerCodeActionProvider("csharp", codeActionProvider);
        }
    }

    private registerDiagnosticsProviders() {
        const provideDiagnostics = (model: monaco.editor.ITextModel) => {
            for (const diagnosticsProvider of this.diagnosticsProviders) {
                diagnosticsProvider.provideDiagnostics(model, markers => {
                    monaco.editor.setModelMarkers(model, model.uri.toString(), markers);
                });
            }
        };

        monaco.editor.onDidCreateModel(model => {
            if (model.getLanguageId() == "csharp") {
                provideDiagnostics(model);
            }

            model.onDidChangeLanguage(ev => {
                if (ev.newLanguage === "csharp") {
                    provideDiagnostics(model);
                }
                else {
                    monaco.editor.removeAllMarkers(model.uri.toString());
                }
            })
        });
    }

    private static lightThemeTokenThemeRules: monaco.editor.ITokenThemeRule[] = [
        {token: "comment", foreground: "008000"},
        {token: "string", foreground: "a31515"},
        {token: "keyword", foreground: "0000ff"},
        {token: "number", foreground: "098658"},
        {token: "regexp", foreground: "EE0000"},
        {token: "operator", foreground: "000000"},
        {token: "namespace", foreground: "267f99"},
        {token: "type", foreground: "267f99"},
        {token: "struct", foreground: "6C9F6C"},
        {token: "class", foreground: "267f99"},
        {token: "interface", foreground: "89b35b"},
        {token: "enum", foreground: "267f99"},
        {token: "typeParameter", foreground: "267f99"},
        {token: "function", foreground: "795E26"},
        {token: "member", foreground: "795E26"},
        // {token: "macro", foreground: "000000"},
        {token: "variable", foreground: "001080"},
        {token: "parameter", foreground: "001080"},
        // {token: "property", foreground: "001080"},
        {token: "enumMember", foreground: "0070C1"},
        {token: "event", foreground: "001080"},
        // {token: "label", foreground: "000000"},
        {token: "plainKeyword", foreground: "0000ff"},
        {token: "controlKeyword", foreground: "AF00DB"},
        {token: "operatorOverloaded", foreground: "795e26"},
        {token: "preprocessorKeyword", foreground: "0000ff"},
        {token: "preprocessorText", foreground: "a31515"},
        {token: "excludedCode", foreground: "BEBEBE"},
        // {token: "punctuation", foreground: "AF00DB"},
        {token: "stringVerbatim", foreground: "a31515"},
        {token: "stringEscapeCharacter", foreground: "EE0000"},
        {token: "delegate", foreground: "267f99"},
        // {token: "module", foreground: "000000"},
        {token: "extensionMethod", foreground: "795E26"},
        {token: "field", foreground: "001080"},
        {token: "local", foreground: "001080"},
        {token: "xmlDocCommentAttributeName", foreground: "008000"},
        {token: "xmlDocCommentAttributeQuotes", foreground: "008000"},
        {token: "xmlDocCommentAttributeValue", foreground: "008000"},
        {token: "xmlDocCommentCDataSection", foreground: "008000"},
        {token: "xmlDocCommentComment", foreground: "008000"},
        {token: "xmlDocCommentDelimiter", foreground: "008000"},
        {token: "xmlDocCommentEntityReference", foreground: "008000"},
        {token: "xmlDocCommentName", foreground: "008000"},
        {token: "xmlDocCommentProcessingInstruction", foreground: "008000"},
        {token: "xmlDocCommentText", foreground: "008000"},
        {token: "regexComment", foreground: "EE0000"},
        {token: "regexCharacterClass", foreground: "EE0000"},
        {token: "regexAnchor", foreground: "EE0000"},
        {token: "regexQuantifier", foreground: "EE0000"},
        {token: "regexGrouping", foreground: "EE0000"},
        {token: "regexAlternation", foreground: "EE0000"},
        {token: "regexSelfEscapedCharacter", foreground: "EE0000"},
        {token: "regexOtherEscape", foreground: "EE0000"},
    ]

    private static darkThemeTokenThemeRules: monaco.editor.ITokenThemeRule[] = [
        {token: "comment", foreground: "6A9955"},
        {token: "string", foreground: "ce9178"},
        {token: "keyword", foreground: "569cd6"},
        {token: "number", foreground: "b5cea8"},
        {token: "regexp", foreground: "D7BA7D"},
        {token: "operator", foreground: "d4d4d4"},
        {token: "namespace", foreground: "4EC9B0"},
        {token: "type", foreground: "4EC9B0"},
        {token: "struct", foreground: "86C691"},
        {token: "class", foreground: "4EC9B0"},
        {token: "interface", foreground: "B8D7A3"},
        {token: "enum", foreground: "B8D7A3"},
        {token: "typeParameter", foreground: "4EC9B0"},
        {token: "function", foreground: "DCDCAA"},
        {token: "member", foreground: "DCDCAA"},
        // {token: "macro", foreground: "FFFFFF"},
        {token: "variable", foreground: "9CDCFE"},
        {token: "parameter", foreground: "9CDCFE"},
        // {token: "property", foreground: "FFFFFF"},
        {token: "enumMember", foreground: "4FC1FF"},
        {token: "event", foreground: "9CDCFE"},
        // {token: "label", foreground: "FFFFFF"},
        {token: "plainKeyword", foreground: "569cd6"},
        {token: "controlKeyword", foreground: "C586C0"},
        {token: "operatorOverloaded", foreground: "dcdcaa"},
        {token: "preprocessorKeyword", foreground: "569cd6"},
        {token: "preprocessorText", foreground: "ce9178"},
        {token: "excludedCode", foreground: "EEEEEE"},
        // {token: "punctuation", foreground: "ffd700"},
        {token: "stringVerbatim", foreground: "ce9178"},
        {token: "stringEscapeCharacter", foreground: "D7BA7D"},
        {token: "delegate", foreground: "4EC9B0"},
        // {token: "module", foreground: "FFFFFF"},
        {token: "extensionMethod", foreground: "DCDCAA"},
        {token: "field", foreground: "9CDCFE"},
        {token: "local", foreground: "9CDCFE"},
        {token: "xmlDocCommentAttributeName", foreground: "6A9955"},
        {token: "xmlDocCommentAttributeQuotes", foreground: "6A9955"},
        {token: "xmlDocCommentAttributeValue", foreground: "6A9955"},
        {token: "xmlDocCommentCDataSection", foreground: "6A9955"},
        {token: "xmlDocCommentComment", foreground: "6A9955"},
        {token: "xmlDocCommentDelimiter", foreground: "6A9955"},
        {token: "xmlDocCommentEntityReference", foreground: "6A9955"},
        {token: "xmlDocCommentName", foreground: "6A9955"},
        {token: "xmlDocCommentProcessingInstruction", foreground: "6A9955"},
        {token: "xmlDocCommentText", foreground: "6A9955"},
        {token: "regexComment", foreground: "D7BA7D"},
        {token: "regexCharacterClass", foreground: "D7BA7D"},
        {token: "regexAnchor", foreground: "D7BA7D"},
        {token: "regexQuantifier", foreground: "D7BA7D"},
        {token: "regexGrouping", foreground: "D7BA7D"},
        {token: "regexAlternation", foreground: "D7BA7D"},
        {token: "regexSelfEscapedCharacter", foreground: "D7BA7D"},
        {token: "regexOtherEscape", foreground: "D7BA7D"},
    ]
}
