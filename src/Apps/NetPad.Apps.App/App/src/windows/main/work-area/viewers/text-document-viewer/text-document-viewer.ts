import {ILogger} from "aurelia";
import {Viewer} from "../viewer";
import {ViewableObject} from "../viewable-object";
import {IEventBus, IScriptService} from "@application";
import {ITextEditor} from "@application/editor/text-editor";
import {ViewableAppScriptDocument, ViewableTextDocument} from "./viewable-text-document";
import {ViewerHost} from "../viewer-host";
import {Workbench} from "../../../workbench";
import {DndType} from "@application/dnd/dnd-type";
import {DragAndDropBase} from "@application/dnd/drag-and-drop-base";
import {DataConnectionDnd} from "@application/dnd/data-connection-dnd";
import {VimStatusbarItem} from "@application/editor/vim/vim-statusbar-item";
import {TextEditorCursorPositionStatusbarItem} from "@application/editor/text-editor-cursor-position-statusbar-item";

export class TextDocumentViewer extends Viewer {
    public editor: ITextEditor;

    constructor(
        host: ViewerHost,
        private readonly workbench: Workbench,
        @IScriptService private readonly scriptService: IScriptService,
        @IEventBus private readonly eventBus: IEventBus,
        logger: ILogger
    ) {
        super(host, logger);
    }

    public attached() {
        if (this.viewable && (!this.editor.active || this.editor.active.id !== this.viewable.id)) {
            this.open(this.viewable as ViewableTextDocument);
        }

        this.workbench.statusbarService.addItem(TextEditorCursorPositionStatusbarItem, "right");
        this.addDisposable(() => this.workbench.statusbarService.removeItem(TextEditorCursorPositionStatusbarItem));

        this.workbench.statusbarService.addItem(VimStatusbarItem, "left");
        this.addDisposable(() => this.workbench.statusbarService.removeItem(VimStatusbarItem));
    }

    public override canOpen(viewableDocument: ViewableObject): boolean {
        return viewableDocument instanceof ViewableTextDocument;
    }

    public open(viewableDocument: ViewableTextDocument) {
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

    public close(viewableDocument: ViewableTextDocument) {
        const logger = this.logger.scopeTo(viewableDocument.toString());
        logger.debug(`Closing app script, looking in cache...`);

        this.editor.close(viewableDocument.textDocument.id);
    }

    public itemDropped(event: DragEvent) {
        const dnd = DragAndDropBase.getFromEventData(event);

        if (dnd?.type == DndType.DataConnection && this.viewable instanceof ViewableAppScriptDocument) {
            this.scriptService.setDataConnection(this.viewable.environment.script.id, (dnd as DataConnectionDnd).dataConnectionId);
        }
    }
}
