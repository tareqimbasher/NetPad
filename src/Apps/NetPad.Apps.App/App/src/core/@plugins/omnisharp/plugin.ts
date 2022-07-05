import {IContainer, Registration} from "aurelia";
import * as monaco from "monaco-editor";
import {
    ICompletionItemProvider,
    IDocumentRangeSemanticTokensProvider,
    IDocumentSemanticTokensProvider, IHoverProvider, IImplementationProvider, IReferenceProvider, ISignatureHelpProvider
} from "@application";
import * as actions from "./actions";
import {IOmniSharpService, OmniSharpService} from "./omnisharp-service";
import {OmnisharpCompletionProvider} from "./omnisharp-completion-provider";
import {OmnisharpSemanticTokensProvider} from "./omnisharp-semantic-tokens-provider";
import {OmnisharpImplementationProvider} from "./omnisharp-implementation-provider";
import {OmnisharpHoverProvider} from "./omnisharp-hover-provider";
import {OmnisharpSignatureHelpProvider} from "./omnisharp-signature-help-provider";
import {OmnisharpReferenceProvider} from "./omnisharp-reference-provider";

/**
 * Encapsulates all OmniSharp functionality.
 */
export class OmniSharpPlugin {
    public static container: IContainer;

    public static configure(container: IContainer) {
        container.register(Registration.singleton(IOmniSharpService, OmniSharpService));
        container.register(Registration.singleton(ICompletionItemProvider, OmnisharpCompletionProvider));
        container.register(Registration.singleton(IDocumentSemanticTokensProvider, OmnisharpSemanticTokensProvider));
        container.register(Registration.cachedCallback(IDocumentRangeSemanticTokensProvider, c => c.get(IDocumentSemanticTokensProvider)));
        container.register(Registration.singleton(IImplementationProvider, OmnisharpImplementationProvider));
        container.register(Registration.singleton(IHoverProvider, OmnisharpHoverProvider));
        container.register(Registration.singleton(ISignatureHelpProvider, OmnisharpSignatureHelpProvider));
        container.register(Registration.singleton(IReferenceProvider, OmnisharpReferenceProvider));

        this.container = container.createChild();

        monaco.editor.onDidCreateEditor(e => {
            const editor = e as monaco.editor.IStandaloneCodeEditor;

            // Actions can only be registered on the model, and the editor model at the time of editor creation
            // does not seem to be the same instance as when the editor is completely configured. Registering
            // the actions on the model of the editor when its first created does not seem to work.
            editor.onDidChangeModel(ev => {
                this.registerActions(editor, [actions.codeFormatAction]);
            });
        });
    }

    private static registerActions(editor: monaco.editor.IStandaloneCodeEditor, actions: monaco.editor.IActionDescriptor[]) {
        for (const action of actions) {
            if (editor.getAction(action.id)) {
                // If action is already registered, don't register again
                return;
            }

            editor.addAction(action);
        }
    }
}
