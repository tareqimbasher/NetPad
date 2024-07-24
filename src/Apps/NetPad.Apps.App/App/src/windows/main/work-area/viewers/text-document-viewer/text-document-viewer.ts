import {Viewer} from "../viewer";
import {ViewableObject} from "../viewable-object";
import {IEventBus, IScriptService} from "@application";
import {ITextEditor} from "@application/editor/text-editor";
import {ILogger} from "aurelia";
import {ViewableAppScriptDocument, ViewableTextDocument} from "./viewable-text-document";
import {ViewerHost} from "../viewer-host";
import {IStatusbarItem} from "../../../statusbar/istatusbar-item";
import {Workbench} from "../../../workbench";
import {DndType} from "@application/dnd/dnd-type";
import {DragAndDropBase} from "@application/dnd/drag-and-drop-base";
import {DataConnectionDnd} from "@application/dnd/data-connection-dnd";

export class TextDocumentViewer extends Viewer {
    public editor: ITextEditor;
    public editorHost: HTMLElement;

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
        this.editor = this.workbench.textEditorService.create(this.editorHost);

        if (this.viewable && (!this.editor.active || this.editor.active.id !== this.viewable.id)) {
            this.open(this.viewable as ViewableTextDocument);
        }

        const item = new TextEditorCursorPositionStatusbarItem(this.editor);
        this.workbench.statusbarService.addItem(item);
        this.addDisposable(() => this.workbench.statusbarService.removeItem(item));
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

class TextEditorCursorPositionStatusbarItem implements IStatusbarItem {
    constructor(private readonly editor: ITextEditor) {
    }

    hoverText = "Go to Line/Column";

    get text(): string {
        return `Ln ${this.editor.position?.lineNumber}, Col ${this.editor.position?.column}`;
    }

    async click() {
        this.editor.monaco.focus();
        this.editor.monaco.trigger(null, "editor.action.gotoLine", null);
    }
}
