import {ITextEditorService} from "@application/editor/itext-editor-service";
import {resolve} from "aurelia";

export class TextEditorCursorPositionStatusbarItem {
    private readonly textEditorService = resolve(ITextEditorService);

    public get text() {
        const editor = this.textEditorService.active;
        if (!editor) {
            return "";
        }

        let text = `Ln ${editor.position?.lineNumber}, Col ${editor.position?.column}`;

        const selection = editor.active?.selection;
        if (selection && !selection.isEmpty()) {
            const lineCount = selection.endLineNumber - selection.startLineNumber + 1;
            if (lineCount > 1) {
                text += ` &nbsp;(${lineCount} lines selected)`;
            } else {
                text += ` &nbsp;(${selection.endColumn - selection.startColumn} selected)`;
            }
        }

        return text;
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
