import {ILogger} from "aurelia";
import {Viewer} from "../viewer";
import {ViewableObject} from "../viewable-object";
import {ITextEditor} from "@application/editor/text-editor";
import {ViewableScriptDocument} from "./viewable-script-document";
import {Workbench} from "../../../workbench";
import {DragAndDropBase} from "@application/dnd/drag-and-drop-base";
import {VimStatusbarItem} from "@application/editor/vim/vim-statusbar-item";
import {TextEditorCursorPositionStatusbarItem} from "@application/editor/text-editor-cursor-position-statusbar-item";

export class ScriptViewer extends Viewer {
    public editor: ITextEditor;

    constructor(
        private readonly workbench: Workbench,
        @ILogger logger: ILogger
    ) {
        super(logger);
    }

    public attached() {
        if (this.viewable && (!this.editor.active || this.editor.active.id !== this.viewable.id)) {
            this.open(this.viewable as ViewableScriptDocument);
        }

        this.workbench.statusbarService.addItem(TextEditorCursorPositionStatusbarItem, "right");
        this.addDisposable(() => this.workbench.statusbarService.removeItem(TextEditorCursorPositionStatusbarItem));

        this.workbench.statusbarService.addItem(VimStatusbarItem, "left");
        this.addDisposable(() => this.workbench.statusbarService.removeItem(VimStatusbarItem));
    }

    public override canOpen(viewableDocument: ViewableObject): boolean {
        return viewableDocument instanceof ViewableScriptDocument;
    }

    public open(viewableDocument: ViewableScriptDocument) {
        if (!this.canOpen(viewableDocument)) {
            this.logger.error(`Cannot open object`, viewableDocument);
            return;
        }

        const logger = this.logger.scopeTo(viewableDocument.toString());

        this.viewable = viewableDocument;

        // This method can be called before the editor is attached
        if (this.editor) {
            logger.debug("Opening in editor");
            this.editor.open(viewableDocument.textDocument);
        } else {
            logger.debug("Editor not ready yet, will not open in editor");
        }
    }

    public close(viewableDocument: ViewableScriptDocument) {
        const logger = this.logger.scopeTo(viewableDocument.toString());
        logger.debug(`Closing app script, looking in cache...`);

        this.editor.close(viewableDocument.textDocument.id);
    }

    public itemDropped(event: DragEvent) {
        const dnd = DragAndDropBase.getFromEventData(event);

        if (this.viewable?.canHandleDrop(dnd)) {
            this.viewable.handleDrop(dnd as DragAndDropBase);
        }
    }
}
