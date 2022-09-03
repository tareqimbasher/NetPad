import Split from "split.js";
import {DataConnectionStore, ISession, Settings} from "@domain";
import {
    BuiltinShortcuts,
    EditorSetup,
    IPaneManager,
    IShortcutManager,
    PaneHost,
    PaneHostOrientation,
} from "@application";
import {ClipboardPane, NamespacesPane} from "./panes";

export class Window {
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
        Split(["sidebar", "script-environments"], {
            gutterSize: 6,
            sizes: [14, 86],
            minSize: [100, 300],
        });

        const viewStateController = {
            split: null,
            expand: (paneHost) => {
                viewStateController.split = Split(["#content-left", `pane-host[data-id='${paneHost.id}']`], {
                    gutterSize: 6,
                    sizes: [85, 15],
                    minSize: [300, 100],
                });
            },
            collapse: (paneHost) => {
                viewStateController.split?.destroy();
            }
        };

        this.rightPaneHost = this.paneManager.createPaneHost(PaneHostOrientation.Right, viewStateController);
        this.paneManager.addPaneToHost(NamespacesPane, this.rightPaneHost);
        this.paneManager.addPaneToHost(ClipboardPane, this.rightPaneHost);
    }

    private registerKeyboardShortcuts() {
        for (const shortcut of BuiltinShortcuts) {
            this.shortcutManager.registerShortcut(shortcut);
        }
    }
}
