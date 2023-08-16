import {IContainer} from "aurelia";
import * as monaco from "monaco-editor";
import {EditorUtil} from "@application";
import {IOmniSharpService} from "./omnisharp-service";

export class Actions {
    constructor(private readonly container: IContainer) {
    }

    public restartOmniSharpServerAction: monaco.editor.IActionDescriptor = {
        id: "netpad.action.omnisharp.restartOmniSharpServer",
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
