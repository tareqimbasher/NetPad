declare module "monaco-vim" {
    import {Constructable} from "aurelia";

    export interface EditorVimMode {
        dispose: () => void;
    }

    export const VimMode: {
        Vim: {
            noremap: (from: string, to: string) => void;
            map: (from: string, to: string, mode: string) => void;
            defineEx: (name: string, shorthand: string | undefined | null, callback: () => void) => void;
        };
    };

    export function initVimMode(
        editor: monaco.editor.IStandaloneCodeEditor,
        statusbarNode: Element | null,
        statusbarClass?: Constructable): EditorVimMode;
}
