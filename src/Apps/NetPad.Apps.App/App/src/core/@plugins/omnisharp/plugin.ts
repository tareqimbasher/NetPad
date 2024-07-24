import {IContainer, Registration} from "aurelia";
import * as monaco from "monaco-editor";
import {
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
    ISignatureHelpProvider,
    Settings
} from "@application";
import {Actions} from "./actions";
import {IOmniSharpService, OmniSharpService} from "./omnisharp-service";
import {OmniSharpCompletionProvider} from "./features/omnisharp-completion-provider";
import {OmniSharpSemanticTokensProvider} from "./features/omnisharp-semantic-tokens-provider";
import {OmniSharpImplementationProvider} from "./features/omnisharp-implementation-provider";
import {OmniSharpHoverProvider} from "./features/omnisharp-hover-provider";
import {OmniSharpSignatureHelpProvider} from "./features/omnisharp-signature-help-provider";
import {OmniSharpReferenceProvider} from "./features/omnisharp-reference-provider";
import {OmniSharpCodeLensProvider} from "./features/omnisharp-code-lens-provider";
import {OmniSharpInlayHintProvider} from "./features/omnisharp-inlay-hint-provider";
import {OmniSharpCodeActionProvider} from "./features/omnisharp-code-action-provider";
import {OmnisharpDiagnosticsProvider} from "./features/omnisharp-diagnostics-provider";
import {OmnisharpDocumentHighlightProvider} from "./features/omnisharp-document-highlight-provider";
import {OmnisharpDocumentSymbolProvider} from "./features/omnisharp-document-symbol-provider";
import {OmnisharpFoldingProvider} from "./features/omnisharp-folding-provider";
import {
    OmnisharpDocumentRangeFormattingEditProvider
} from "./features/omnisharp-document-range-formatting-edit-provider";
import {OmnisharpOnTypeFormattingEditProvider} from "./features/omnisharp-on-type-formatting-edit-provider";
import {OmnisharpRenameProvider} from "./features/omnisharp-rename-provider";

/**
 * Encapsulates all OmniSharp functionality.
 */
export function configure(container: IContainer) {
    const settings = container.get(Settings);

    container.register(Registration.singleton(IOmniSharpService, OmniSharpService));
    container.register(Registration.singleton(IImplementationProvider, OmniSharpImplementationProvider));
    container.register(Registration.singleton(IHoverProvider, OmniSharpHoverProvider));
    container.register(Registration.singleton(ISignatureHelpProvider, OmniSharpSignatureHelpProvider));
    container.register(Registration.singleton(IReferenceProvider, OmniSharpReferenceProvider));
    container.register(Registration.singleton(IDocumentHighlightProvider, OmnisharpDocumentHighlightProvider));
    container.register(Registration.singleton(IInlayHintsProvider, OmniSharpInlayHintProvider));
    container.register(Registration.singleton(IDocumentSymbolProvider, OmnisharpDocumentSymbolProvider));
    container.register(Registration.singleton(IFoldingRangeProvider, OmnisharpFoldingProvider));
    container.register(Registration.singleton(IDocumentRangeFormattingEditProvider, OmnisharpDocumentRangeFormattingEditProvider));
    container.register(Registration.singleton(IOnTypeFormattingEditProvider, OmnisharpOnTypeFormattingEditProvider));
    container.register(Registration.singleton(IRenameProvider, OmnisharpRenameProvider));

    container.register(Registration.singleton(OmniSharpCompletionProvider, OmniSharpCompletionProvider));
    container.register(Registration.cachedCallback(ICompletionItemProvider, c => c.get(OmniSharpCompletionProvider)));
    container.register(Registration.cachedCallback(ICommandProvider, c => c.get(OmniSharpCompletionProvider)));

    container.register(Registration.singleton(OmniSharpCodeActionProvider, OmniSharpCodeActionProvider));
    container.register(Registration.cachedCallback(ICodeActionProvider, c => c.get(OmniSharpCodeActionProvider)));
    container.register(Registration.cachedCallback(ICommandProvider, c => c.get(OmniSharpCodeActionProvider)));

    if (settings.omniSharp.enableCodeLensReferences) {
        container.register(Registration.singleton(ICodeLensProvider, OmniSharpCodeLensProvider));
    }

    if (settings.omniSharp.enableSemanticHighlighting) {
        container.register(Registration.singleton(OmniSharpSemanticTokensProvider, OmniSharpSemanticTokensProvider));
        container.register(Registration.cachedCallback(IDocumentSemanticTokensProvider, c => c.get(OmniSharpSemanticTokensProvider)));
        container.register(Registration.cachedCallback(IDocumentRangeSemanticTokensProvider, c => c.get(OmniSharpSemanticTokensProvider)));
    }

    if (settings.omniSharp.diagnostics.enabled) {
        container.register(Registration.singleton(IDiagnosticsProvider, OmnisharpDiagnosticsProvider));
    }

    const actions = new Actions(container);

    monaco.editor.onDidCreateEditor(e => {
        const editor = e as monaco.editor.IStandaloneCodeEditor;

        // Check if editor is a IStandaloneCodeEditor
        if (!editor.addAction) {
            return;
        }

        // Actions can only be registered on the model, and the editor model at the time of editor creation
        // does not seem to be the same instance as when the editor is completely configured. Registering
        // the actions on the model of the editor when its first created does not seem to work.
        editor.onDidChangeModel(ev => {
            registerActions(editor, [
                actions.restartOmniSharpServerAction,
            ]);
        });
    });
}

function registerActions(editor: monaco.editor.IStandaloneCodeEditor, actions: monaco.editor.IActionDescriptor[]) {
    for (const action of actions) {
        if (editor.getAction(action.id)) {
            // If action is already registered, don't register again
            continue;
        }

        editor.addAction(action);
    }
}
