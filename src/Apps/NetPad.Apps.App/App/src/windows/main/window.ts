import {DataConnectionStore, ISession, Settings} from "@domain";
import {
    BuiltinShortcuts,
    EditorSetup,
    IPaneManager,
    IShortcutManager,
    PaneHost,
    PaneHostOrientation,
} from "@application";
import {ClipboardPane, Explorer, NamespacesPane} from "./panes";
import {LeftRightPaneHostViewStateController} from "./left-right-pane-host-view-state-controller";
import {PLATFORM} from "aurelia";

export class Window {
    public leftPaneHost: PaneHost;
    public rightPaneHost: PaneHost;

    constructor(
        private readonly settings: Settings,
        @ISession private readonly session: ISession,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @IPaneManager private readonly paneManager: IPaneManager,
        private readonly dataConnectionStore: DataConnectionStore,
        private readonly editorSetup: EditorSetup) {
    }

    public async binding() {
        this.shortcutManager.initialize();
        this.registerKeyboardShortcuts();
        this.editorSetup.setup();

        await this.session.initialize();
        await this.dataConnectionStore.initialize();
    }

    public attached() {
        const viewStateController = new LeftRightPaneHostViewStateController(
            "#main-content",
            () => this.leftPaneHost,
            () => this.rightPaneHost
        );

        this.leftPaneHost = this.paneManager.createPaneHost(PaneHostOrientation.Left, viewStateController);
        const explorer = this.paneManager.addPaneToHost(Explorer, this.leftPaneHost);

        this.rightPaneHost = this.paneManager.createPaneHost(PaneHostOrientation.Right, viewStateController);
        this.paneManager.addPaneToHost(NamespacesPane, this.rightPaneHost);
        this.paneManager.addPaneToHost(ClipboardPane, this.rightPaneHost);

        // Start explorer expanded by default
        if (!viewStateController.hasSavedState()) {
            PLATFORM.taskQueue.queueTask(() => explorer.activateOrCollapse(), {delay: 1});
        }
    }

    private registerKeyboardShortcuts() {
        for (const shortcut of BuiltinShortcuts) {
            this.shortcutManager.registerShortcut(shortcut);
        }
    }
}
