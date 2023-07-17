import * as monaco from "monaco-editor";
import {IQuickInputService} from 'monaco-editor/esm/vs/platform/quickinput/common/quickInput';
import {IKeybindingService} from 'monaco-editor/esm/vs/platform/keybinding/common/keybinding';
import {StandaloneServices} from 'monaco-editor/esm/vs/editor/standalone/browser/standaloneServices';

export class EditorUtil {
    public static constructModelUri(scriptId: string): monaco.Uri {
        return monaco.Uri.from({
            scheme: "inmemory",     // This is what monaco sets 'scheme' when uri is auto-generated
            authority: "model",     // This is what monaco sets 'authority' when uri is auto-generated
            path: `/${scriptId}`    // Must start with a '/'
        });
    }

    public static getScriptId(textModel: monaco.editor.ITextModel): string {
        return textModel.uri.path.substring(1);
    }

    public static getQuickInputService(): IQuickInputService {
        return StandaloneServices.get(IQuickInputService);
    }

    public static getKeybindingService() : IKeybindingService {
        return StandaloneServices.get(IKeybindingService);
    }
}
