declare module "monaco-vim" {
    import {Constructable} from "aurelia";

    export interface VimMode {
        dispose(): void;
    }

    export function initVimMode(
        editor: monaco.editor.IStandaloneCodeEditor,
        statusbarNode: Element | null,
        statusbarClass?: Constructable): VimMode;
}
