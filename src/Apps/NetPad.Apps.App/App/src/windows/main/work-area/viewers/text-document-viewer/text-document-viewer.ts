import {Viewer} from "../viewer";
import {ViewableObject} from "../viewable-object";
import {IEventBus, IScriptService} from "@domain";
import {ITextEditor} from "@application/editor/text-editor";
import {ILogger} from "aurelia";
import {ViewableTextDocument} from "./viewable-text-document";
import {ViewerHost} from "../viewer-host";

export class TextDocumentViewer extends Viewer {
    public editor: ITextEditor;

    constructor(
        host: ViewerHost,
        @IScriptService private readonly scriptService: IScriptService,
        @IEventBus private readonly eventBus: IEventBus,
        logger: ILogger
    ) {
        super(host, logger);
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

    public attached() {
        if (this.viewable && (!this.editor.active || this.editor.active.id !== this.viewable.id)) {
            this.open(this.viewable as ViewableTextDocument);
        }
    }
}
