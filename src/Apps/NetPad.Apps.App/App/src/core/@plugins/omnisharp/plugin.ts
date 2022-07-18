import {IContainer, Registration} from "aurelia";
import * as monaco from "monaco-editor";
import {
    ICodeActionProvider,
    ICodeLensProvider,
    ICommandProvider,
    ICompletionItemProvider,
    IDocumentRangeSemanticTokensProvider,
    IDocumentSemanticTokensProvider,
    IHoverProvider,
    IImplementationProvider,
    IInlayHintsProvider,
    IReferenceProvider,
    ISignatureHelpProvider
} from "@application";
import {Actions} from "./actions";
import {IOmniSharpService, OmniSharpService} from "./omnisharp-service";
import {OmnisharpCompletionProvider} from "./features/omnisharp-completion-provider";
import {OmnisharpSemanticTokensProvider} from "./features/omnisharp-semantic-tokens-provider";
import {OmnisharpImplementationProvider} from "./features/omnisharp-implementation-provider";
import {OmnisharpHoverProvider} from "./features/omnisharp-hover-provider";
import {OmnisharpSignatureHelpProvider} from "./features/omnisharp-signature-help-provider";
import {OmnisharpReferenceProvider} from "./features/omnisharp-reference-provider";
import {OmnisharpCodeLensProvider} from "./features/omnisharp-code-lens-provider";
import {OmnisharpInlayHintProvider} from "./features/omnisharp-inlay-hint-provider";
import {OmnisharpCodeActionProvider} from "./features/omnisharp-code-action-provider";

/**
 * Encapsulates all OmniSharp functionality.
 */

export function configure(container: IContainer) {
    container.register(Registration.singleton(IOmniSharpService, OmniSharpService));
    container.register(Registration.singleton(IImplementationProvider, OmnisharpImplementationProvider));
    container.register(Registration.singleton(IHoverProvider, OmnisharpHoverProvider));
    container.register(Registration.singleton(ISignatureHelpProvider, OmnisharpSignatureHelpProvider));
    container.register(Registration.singleton(IReferenceProvider, OmnisharpReferenceProvider));
    container.register(Registration.singleton(ICodeLensProvider, OmnisharpCodeLensProvider));
    container.register(Registration.singleton(IInlayHintsProvider, OmnisharpInlayHintProvider));

    container.register(Registration.singleton(OmnisharpSemanticTokensProvider, OmnisharpSemanticTokensProvider));
    container.register(Registration.cachedCallback(IDocumentSemanticTokensProvider, c => c.get(OmnisharpSemanticTokensProvider)));
    container.register(Registration.cachedCallback(IDocumentRangeSemanticTokensProvider, c => c.get(OmnisharpSemanticTokensProvider)));

    container.register(Registration.singleton(OmnisharpCompletionProvider, OmnisharpCompletionProvider));
    container.register(Registration.cachedCallback(ICompletionItemProvider, c => c.get(OmnisharpCompletionProvider)));
    container.register(Registration.cachedCallback(ICommandProvider, c => c.get(OmnisharpCompletionProvider)));

    container.register(Registration.singleton(OmnisharpCodeActionProvider, OmnisharpCodeActionProvider));
    container.register(Registration.cachedCallback(ICodeActionProvider, c => c.get(OmnisharpCodeActionProvider)));
    container.register(Registration.cachedCallback(ICommandProvider, c => c.get(OmnisharpCodeActionProvider)));

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
                actions.codeFormatAction,
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
