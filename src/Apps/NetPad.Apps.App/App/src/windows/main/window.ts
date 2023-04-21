import {DataConnectionStore, ISession, Settings} from "@domain";
import {
    BuiltinShortcuts,
    EditorSetup,
    IPaneManager,
    IShortcutManager,
    PaneHost,
    PaneHostOrientation,
} from "@application";
import {ClipboardPane, Explorer, NamespacesPane, OutputPane, PaneHostViewStateController} from "./panes";
import {Workbench} from "./workbench";
import {IContainer} from "aurelia";

export class Window {
    private workbench: Workbench
    public leftPaneHost: PaneHost;
    public rightPaneHost: PaneHost;
    public bottomPaneHost: PaneHost;

    constructor(
        @ISession private readonly session: ISession,
        @IShortcutManager private readonly shortcutManager: IShortcutManager,
        @IPaneManager private readonly paneManager: IPaneManager,
        @IContainer private readonly container: IContainer,
        private readonly settings: Settings,
        private readonly dataConnectionStore: DataConnectionStore,
        private readonly editorSetup: EditorSetup) {
    }

    public hydrating() {
        this.shortcutManager.initialize();
        this.registerKeyboardShortcuts();
    }

    public async binding() {
        this.editorSetup.setup();
        await this.session.initialize();
        await this.dataConnectionStore.initialize();
        this.workbench = this.container.get(Workbench);
    }

    public attached() {
        const middleContentElement = document.getElementById("window-middle-content");
        const workAreaElement = middleContentElement?.querySelector("work-area") as HTMLElement;

        if (!middleContentElement || !workAreaElement) throw new Error("Could not find required elements");

        const sideToSideController = new PaneHostViewStateController(
            () => [this.leftPaneHost, middleContentElement, this.rightPaneHost],
            "horizontal",
            15
        );

        this.leftPaneHost = this.paneManager.createPaneHost(PaneHostOrientation.Left, sideToSideController);
        this.rightPaneHost = this.paneManager.createPaneHost(PaneHostOrientation.Right, sideToSideController);

        const explorer = this.paneManager.addPaneToHost(Explorer, this.leftPaneHost);
        this.paneManager.addPaneToHost(NamespacesPane, this.rightPaneHost);
        this.paneManager.addPaneToHost(ClipboardPane, this.rightPaneHost);


        const topBottomController = new PaneHostViewStateController(
            () => [workAreaElement, this.bottomPaneHost],
            "vertical",
            50
        );

        this.bottomPaneHost = this.paneManager.createPaneHost(PaneHostOrientation.Bottom, topBottomController);
        this.paneManager.addPaneToHost(OutputPane, this.bottomPaneHost);


        // Start explorer expanded by default
        if (!sideToSideController.hasSavedState()) {
            setTimeout(() => explorer.activate(), 1);
        }
    }

    private registerKeyboardShortcuts() {
        for (const shortcut of BuiltinShortcuts) {
            this.shortcutManager.registerShortcut(shortcut);
        }
    }
}
