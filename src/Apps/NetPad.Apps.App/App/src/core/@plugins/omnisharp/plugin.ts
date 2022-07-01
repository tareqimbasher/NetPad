import {IContainer, Registration} from "aurelia";
import * as monaco from "monaco-editor";
import {EditorUtil, ICompletionItemProvider} from "@application";
import {CodeFormatRequest} from "@domain";
import {OmnisharpCompletionProvider} from "./omnisharp-completion-provider";
import {IOmniSharpService, OmniSharpService} from "./omnisharp-service";

/**
 * Encapsulates all OmniSharp functionality.
 */
export class OmniSharpPlugin {
    private static container: IContainer;

    public static configure(container: IContainer) {
        container.register(Registration.singleton(IOmniSharpService, OmniSharpService));
        container.register(Registration.singleton(ICompletionItemProvider, OmnisharpCompletionProvider));

        this.container = container.createChild();

        monaco.editor.onDidCreateEditor(e => {
            const editor = e as monaco.editor.IStandaloneCodeEditor;

            editor.onDidChangeModel(ev => {
                const actions = [ this.codeFormatAction ];

                for (const action of actions) {
                    if (editor.getAction(action.id)) {
                        // If action is already registered, don't register again
                        return;
                    }

                    editor.addAction(action);
                }
            });
        });
    }

    private static codeFormatAction: monaco.editor.IActionDescriptor = {
        id: "netpad-code-format",
        label: "Format Code",
        keybindings: [
            monaco.KeyMod.CtrlCmd | monaco.KeyMod.Shift | monaco.KeyCode.KeyI
        ],
        contextMenuGroupId: 'navigation',
        contextMenuOrder: 1.5,
        run: async (ed) => {
            const cursorPos = ed.getPosition();

            const request = new CodeFormatRequest(<any>{
                buffer: ed.getModel().getValue()
            });

            const scriptId = EditorUtil.getScriptId(ed.getModel());
            const omnisharpService = OmniSharpPlugin.container.get(IOmniSharpService);
            const response = await omnisharpService.formatCode(scriptId, request);

            ed.setValue(response.buffer);
            ed.setPosition(cursorPos);
        }
    }
}
