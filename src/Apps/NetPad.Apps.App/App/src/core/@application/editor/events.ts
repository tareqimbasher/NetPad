import {ITextEditor} from "@application/editor/text-editor";

export class TextEditorFocusedEvent {
    constructor(public readonly editor: ITextEditor) {
    }
}
