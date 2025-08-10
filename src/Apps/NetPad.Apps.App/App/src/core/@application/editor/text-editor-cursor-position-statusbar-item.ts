import {ITextEditorService} from "@application/editor/itext-editor-service";
import {resolve} from "aurelia";

export class TextEditorCursorPositionStatusbarItem {
    private readonly textEditorService = resolve(ITextEditorService);

    public get text() {
        const editor = this.textEditorService.active;
        if (!editor) {
            return "";
        }

        return `Ln ${editor.position?.lineNumber}, Col ${editor.position?.column}`;
    }

    public goToLine() {
        const editor = this.textEditorService.active;
        if (!editor) {
            return;
        }

        editor.monaco.focus();
        editor.monaco.trigger(null, "editor.action.gotoLine", null);
    }
}
