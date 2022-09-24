import * as monaco from "monaco-editor";
import {EditorUtil} from "@application";
import {IOmniSharpService} from "./omnisharp-service";
import {CodeFormatRequest} from "./api";
import {IContainer} from "aurelia";

export class Actions {
    constructor(private readonly container: IContainer) {
    }

    public codeFormatAction: monaco.editor.IActionDescriptor = {
        id: "netpad-omnisharp-code-format",
        label: "Format Code",
        keybindings: [
            monaco.KeyMod.CtrlCmd | monaco.KeyMod.Shift | monaco.KeyCode.KeyI
        ],
        contextMenuGroupId: "1_modification",
        contextMenuOrder: 2,
        run: async (editor) => {
            const model = editor.getModel();
            if (!model) return;

            const cursorPos = editor.getPosition();
            const editorValue = model.getValue();

            const request = new CodeFormatRequest({
                buffer: editorValue
            } as unknown as CodeFormatRequest);

            const scriptId = EditorUtil.getScriptId(model);
            const omnisharpService = this.container.get(IOmniSharpService);
            const response = await omnisharpService.formatCode(scriptId, request);

            if (response.buffer === editorValue) {
                return;
            }

            model.pushEditOperations([], [
                {
                    text: response.buffer || null,
                    range: model.getFullModelRange(),
                    forceMoveMarkers: false
                }
            ], () => []);
            model.pushStackElement();

            if (cursorPos) editor.setPosition(cursorPos);
        }
    }

    public restartOmniSharpServerAction: monaco.editor.IActionDescriptor = {
        id: "netpad-omnisharp-restart-server",
        label: "Developer: Restart OmniSharp Server",
        run: async (editor) => {
            const model = editor.getModel();
            if (!model) return;

            const scriptId = EditorUtil.getScriptId(model);

            const omnisharpService = this.container.get(IOmniSharpService);
            await omnisharpService.restartServer(scriptId);
        }
    }
}
